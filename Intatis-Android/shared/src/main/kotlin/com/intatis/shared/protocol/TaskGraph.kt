package com.intatis.shared.protocol

data class TaskNode(
    val id: TaskID,
    val contract: TaskContract,
    val status: TaskStatus,
    val rootTaskID: TaskID,
    val parentTaskID: TaskID? = null,
    val issuer: AgentID? = null,
    val assignee: AgentID,
    val createdAt: String
)

enum class TaskEdgeKind {
    delegates,
    requestsInformation,
    replies,
    blocks
}

data class TaskEdge(
    val fromTaskID: TaskID,
    val toTaskID: TaskID,
    val issuer: AgentID? = null,
    val assignee: AgentID,
    val kind: TaskEdgeKind
)

data class TaskGraphPolicy(
    val maxTaskDepth: Int = 4,
    val maxDelegationHops: Int = 4,
    val maxTasksPerRoot: Int = 32,
    val maxActiveAgentsPerThread: Int = 8
) {
    companion object {
        val default: TaskGraphPolicy = TaskGraphPolicy()
    }
}

data class TaskGraphViolation(
    val kind: Kind,
    val message: String,
    val taskID: TaskID? = null,
    val existingTaskID: TaskID? = null
) {
    enum class Kind {
        selfDelegation,
        cycleDetected,
        maxDepthExceeded,
        maxDelegationHopsExceeded,
        maxTasksPerRootExceeded,
        maxActiveAgentsExceeded,
        duplicateTask,
        missingParentTask,
        duplicateTaskID
    }
}

data class TaskGraphAdmission(
    val node: TaskNode,
    val edge: TaskEdge? = null,
    val rootTaskID: TaskID,
    val hopCount: Int,
    val visitedAgents: List<AgentID>
)

sealed interface TaskGraphAdmissionResult {
    val admission: TaskGraphAdmission?
    val violation: TaskGraphViolation?
}

data class TaskGraphAdmissionSuccess(override val admission: TaskGraphAdmission, override val violation: TaskGraphViolation? = null) :
    TaskGraphAdmissionResult

data class TaskGraphAdmissionFailure(override val admission: TaskGraphAdmission? = null, override val violation: TaskGraphViolation) :
    TaskGraphAdmissionResult

data class TaskGraph(
    private val nodes: MutableMap<TaskID, TaskNode> = mutableMapOf(),
    private val edges: MutableList<TaskEdge> = mutableListOf(),
    val policy: TaskGraphPolicy = TaskGraphPolicy.default
) {
    fun node(id: TaskID): TaskNode? = nodes[id]

    fun addRootTask(contract: TaskContract): TaskGraphAdmissionResult {
        val taskID = contract.id
        if (nodes[taskID] != null) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.duplicateTaskID,
                    message = "task id already exists: $taskID",
                    taskID = taskID,
                    existingTaskID = taskID
                )
            )
        }
        val node = TaskNode(
            id = taskID,
            contract = contract,
            status = TaskStatus.created,
            rootTaskID = taskID,
            parentTaskID = null,
            issuer = contract.issuer,
            assignee = contract.assignee,
            createdAt = System.currentTimeMillis().toString()
        )
        val admission = TaskGraphAdmission(
            node = node,
            rootTaskID = taskID,
            hopCount = if (contract.issuer == null) 0 else 1,
            visitedAgents = uniqueAgents(listOfNotNull(contract.issuer, contract.assignee))
        )
        nodes[taskID] = node
        return TaskGraphAdmissionSuccess(admission)
    }

    fun validateAddTask(contract: TaskContract): TaskGraphAdmissionResult {
        val taskID = contract.id
        if (nodes[taskID] != null) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.duplicateTaskID,
                    message = "task id already exists: $taskID",
                    taskID = taskID,
                    existingTaskID = taskID
                )
            )
        }
        if (contract.issuer == contract.assignee) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.selfDelegation,
                    message = "agent cannot delegate to itself",
                    taskID = taskID
                )
            )
        }
        val parentNode = contract.parentTaskID?.let(nodes::get)
        if (contract.parentTaskID != null && parentNode == null) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.missingParentTask,
                    message = "parent task does not exist: ${contract.parentTaskID}",
                    taskID = taskID
                )
            )
        }

        val rootTaskID = parentNode?.rootTaskID ?: taskID
        val taskDepth = if (parentNode == null) 1 else (depth(parentNode.id) + 1)
        if (taskDepth > policy.maxTaskDepth) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.maxDepthExceeded,
                    message = "task depth $taskDepth exceeds limit ${policy.maxTaskDepth}",
                    taskID = taskID
                )
            )
        }

        var visitedAgents = parentNode?.let { causalAgentChain(it.id) } ?: emptyList()
        val issuer = contract.issuer
        if (issuer != null && visitedAgents.lastOrNull() != issuer) {
            visitedAgents = visitedAgents + issuer
        }
        if (visitedAgents.contains(contract.assignee)) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.cycleDetected,
                    message = "delegation cycle rejected for @${contract.assignee}",
                    taskID = taskID
                )
            )
        }
        visitedAgents = uniqueAgents(visitedAgents + contract.assignee)
        val hopCount = kotlin.math.max(0, visitedAgents.size - 1)
        if (hopCount > policy.maxDelegationHops) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.maxDelegationHopsExceeded,
                    message = "delegation hops $hopCount exceeds limit ${policy.maxDelegationHops}",
                    taskID = taskID
                )
            )
        }
        val tasksForRoot = nodes.values.count { it.rootTaskID == rootTaskID } + 1
        if (tasksForRoot > policy.maxTasksPerRoot) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.maxTasksPerRootExceeded,
                    message = "root task $rootTaskID exceeds task limit ${policy.maxTasksPerRoot}",
                    taskID = taskID
                )
            )
        }
        val activeAgents = mutableSetOf<AgentID>()
        nodes.values.filter { isActive(it.status) }.forEach { activeAgents.add(it.assignee) }
        activeAgents.add(contract.assignee)
        if (activeAgents.size > policy.maxActiveAgentsPerThread) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.maxActiveAgentsExceeded,
                    message = "active agent count ${activeAgents.size} exceeds limit ${policy.maxActiveAgentsPerThread}",
                    taskID = taskID
                )
            )
        }

        val duplicate = duplicateActiveTask(contract)
        if (duplicate != null) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.duplicateTask,
                    message = "duplicate active task rejected",
                    taskID = taskID,
                    existingTaskID = duplicate.id
                )
            )
        }

        val edge = parentNode?.let {
            TaskEdge(
                fromTaskID = it.id,
                toTaskID = taskID,
                issuer = contract.issuer,
                assignee = contract.assignee,
                kind = TaskEdgeKind.delegates
            )
        }
        if (edge != null && wouldCreateTaskCycle(edge)) {
            return TaskGraphAdmissionFailure(
                violation = TaskGraphViolation(
                    kind = TaskGraphViolation.Kind.cycleDetected,
                    message = "task edge would create a cycle",
                    taskID = taskID
                )
            )
        }

        val node = TaskNode(
            id = taskID,
            contract = contract,
            status = TaskStatus.created,
            rootTaskID = rootTaskID,
            parentTaskID = contract.parentTaskID,
            issuer = contract.issuer,
            assignee = contract.assignee,
            createdAt = System.currentTimeMillis().toString()
        )
        return TaskGraphAdmissionSuccess(
            TaskGraphAdmission(
                node = node,
                edge = edge,
                rootTaskID = rootTaskID,
                hopCount = hopCount,
                visitedAgents = visitedAgents
            )
        )
    }

    fun addTask(contract: TaskContract): TaskGraphAdmissionResult {
        val result = validateAddTask(contract)
        if (result is TaskGraphAdmissionSuccess) {
            val admission = result.admission ?: return result
            nodes[admission.node.id] = admission.node
            admission.edge?.let(edges::add)
            return result
        }
        return result
    }

    fun updateStatus(taskID: TaskID, status: TaskStatus) {
        val current = nodes[taskID] ?: return
        nodes[taskID] = current.copy(status = status)
    }

    fun causalAgentChain(taskID: TaskID): List<AgentID> {
        val node = nodes[taskID] ?: return emptyList()
        val chain = mutableListOf<AgentID>()
        node.parentTaskID?.let { chain.addAll(causalAgentChain(it)) }
        node.issuer?.let {
            if (chain.lastOrNull() != it) chain.add(it)
        }
        if (chain.lastOrNull() != node.assignee) chain.add(node.assignee)
        return uniqueAgents(chain)
    }

    fun depth(taskID: TaskID): Int {
        val node = nodes[taskID] ?: return 0
        return if (node.parentTaskID == null) 1 else 1 + depth(node.parentTaskID)
    }

    private fun duplicateActiveTask(contract: TaskContract): TaskNode? {
        val objective = normalizedObjective(contract.objective)
        return nodes.values.firstOrNull { node ->
            if (!isActive(node.status)) return@firstOrNull false
            if (node.parentTaskID != contract.parentTaskID) return@firstOrNull false
            if (node.assignee != contract.assignee) return@firstOrNull false
            if (normalizedObjective(node.contract.objective) != objective) return@firstOrNull false
            sameWorkspaceScope(node.contract, contract)
        }
    }

    private fun wouldCreateTaskCycle(newEdge: TaskEdge): Boolean {
        val adjacency = mutableMapOf<TaskID, MutableList<TaskID>>()
        for (edge in edges) {
            if (edge.kind == TaskEdgeKind.delegates) {
                adjacency.getOrPut(edge.fromTaskID) { mutableListOf() }.add(edge.toTaskID)
            }
        }
        adjacency.getOrPut(newEdge.fromTaskID) { mutableListOf() }.add(newEdge.toTaskID)
        return hasPath(newEdge.toTaskID, newEdge.fromTaskID, adjacency)
    }

    private fun hasPath(start: TaskID, target: TaskID, adjacency: Map<TaskID, List<TaskID>>): Boolean {
        if (start == target) return true
        val visited = mutableSetOf<TaskID>()
        val stack = ArrayDeque<TaskID>()
        stack.add(start)
        while (stack.isNotEmpty()) {
            val current = stack.removeLast()
            if (current == target) return true
            if (!visited.add(current)) continue
            adjacency[current]?.forEach(stack::addLast)
        }
        return false
    }

    private fun sameWorkspaceScope(lhs: TaskContract, rhs: TaskContract): Boolean {
        return when {
            lhs.workspaceLeaseID != null && rhs.workspaceLeaseID != null -> lhs.workspaceLeaseID == rhs.workspaceLeaseID
            lhs.workspaceLeaseID == null && rhs.workspaceLeaseID == null -> lhs.workspaceID == null || lhs.workspaceID == rhs.workspaceID
            else -> false
        }
    }

    private fun isActive(status: TaskStatus): Boolean =
        status == TaskStatus.created || status == TaskStatus.assigned || status == TaskStatus.queued || status == TaskStatus.running

    private fun uniqueAgents(agents: List<AgentID>): List<AgentID> {
        val seen = linkedSetOf<AgentID>()
        seen.addAll(agents)
        return seen.toList()
    }

    private fun normalizedObjective(value: String): String =
        value.trim().lowercase().replace("\\s+".toRegex(), " ")
}
