package com.intatis.shared

import com.intatis.shared.protocol.TaskContract
import com.intatis.shared.protocol.TaskGraph
import com.intatis.shared.protocol.TaskGraphAdmissionFailure
import com.intatis.shared.protocol.TaskGraphAdmissionSuccess
import com.intatis.shared.protocol.TaskStatus
import kotlinx.coroutines.runBlocking

/**
 * Android cowork orchestration facade.
 *
 * This keeps the current engine behavior intact while adding the canonical
 * `CoworkOrchestrator` entrypoint expected by the migration plan.
 */
class CoworkOrchestrator(
    config: IntatisConfig,
    baseWorkspace: String,
    shell: IToolShellRunner? = null,
    git: IToolGitService? = null,
    responder: IPermissionResponder = AllowAllResponder(),
    profile: PermissionProfile = PermissionProfile.Reviewed,
    eventSink: IConversationEventSink = NullConversationEventSink(),
    messageBus: CoworkMessageBus = CoworkMessageBus(),
    allowsShell: Boolean = true,
    maxIterations: Int = 8,
    permissionReviewer: IPermissionReviewer? = null,
    private val taskGraph: TaskGraph = TaskGraph(),
) {
    private val eventSink: IConversationEventSink = eventSink
    private val messageBus: CoworkMessageBus = messageBus
    private val engine = CoworkEngine(
        config,
        baseWorkspace,
        shell = shell,
        git = git,
        responder = responder,
        profile = profile,
        eventSink = eventSink,
        messageBus = messageBus,
        allowsShell = allowsShell,
        maxIterations = maxIterations,
        permissionReviewer = permissionReviewer ?: ModelPermissionReviewer(config),
    )
    private val scheduler = AgentScheduler()
    private val scheduledReplyTargets = mutableMapOf<String, String>()

    val agentsNames: List<String> get() = engine.agentsNames
    val queuedTaskCount: Int get() = scheduler.queuedTaskCount()

    fun send(text: String, targetAgent: String?, model: String? = null, reasoning: String? = null): String =
        engine.send(text, targetAgent, model, reasoning)

    suspend fun sendAsync(
        text: String,
        targetAgent: String?,
        model: String? = null,
        reasoning: String? = null,
        images: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
    ): String = engine.sendAsync(
        text = text,
        targetAgent = targetAgent,
        model = model,
        reasoning = reasoning,
        images = images,
        includeUsage = includeUsage,
    )

    suspend fun askAsync(
        from: String,
        to: String,
        question: String,
        userGoal: String? = null,
        images: List<ImageAttachment> = emptyList(),
        includeUsage: Boolean = false,
    ): String = enqueueAsk(
        from = from,
        to = to,
        question = question,
    )

    suspend fun sendMessageAsync(
        from: String,
        to: String,
        content: String,
    ): String = engine.sendMessageAsync(from, to, content)

    suspend fun requestInformationAsync(
        from: String,
        to: String,
        question: String,
        taskID: String? = null,
    ): String = engine.requestInformation(from, to, question, taskID)

    suspend fun replyMessageAsync(
        from: String,
        to: String,
        answer: String,
        inReplyTo: String? = null,
        taskID: String? = null,
    ): String = engine.replyMessage(from, to, answer, inReplyTo, taskID)

    suspend fun requestDelegationAsync(
        from: String,
        objective: String,
        reason: String = "delegation requested",
    ): String = engine.requestDelegation(from, objective, reason)

    suspend fun delegateTaskAsync(
        from: String,
        to: String,
        objective: String,
        reason: String = "delegation requested",
        roleHint: String = "cowork",
        expectedDeliverable: String = "response",
        parentTaskID: String? = null,
    ): String = engine.delegateTask(from, to, objective, reason)
        .let { result ->
            if (!result.startsWith("delegation")) {
                return@let result
            }
            val normalizedFrom = if (from.isBlank()) null else from
            val normalizedParentTaskID = parentTaskID?.trim().ifBlank { null }
            val normalizedRoleHint = roleHint.trim().ifEmpty { "cowork" }
            val normalizedExpectedDeliverable = expectedDeliverable.trim().ifEmpty { "response" }
            val contract = TaskContract(
                assignee = to,
                issuer = normalizedFrom,
                parentTaskID = normalizedParentTaskID,
                objective = objective,
                roleHint = normalizedRoleHint,
                expectedDeliverable = normalizedExpectedDeliverable,
            )
            when (val admission = taskGraph.validateAddTask(contract)) {
                is TaskGraphAdmissionFailure -> {
                    emitTaskRejected(
                        contract = contract,
                        requester = normalizedFrom,
                        assignee = to,
                        objective = objective,
                        reason = admission.violation.message,
                        violationKind = admission.violation.kind.name
                    )
                    "task rejected: ${admission.violation.message}"
                }

                is TaskGraphAdmissionSuccess -> {
                    val addResult = taskGraph.addTask(contract)
                    when (addResult) {
                        is TaskGraphAdmissionFailure -> {
                            emitTaskRejected(
                                contract = contract,
                                requester = normalizedFrom,
                                assignee = to,
                                objective = objective,
                                reason = addResult.violation.message,
                                violationKind = addResult.violation.kind.name
                            )
                            "task rejected: ${addResult.violation.message}"
                        }

                        is TaskGraphAdmissionSuccess -> {
                            val admissionResult = addResult.admission
                            val scheduled = ScheduledTask(
                                contract = contract,
                                input = "[task ${contract.id}] ${contract.objective}",
                                rootTaskID = admissionResult.rootTaskID,
                                parentTaskID = contract.parentTaskID,
                                issuer = normalizedFrom,
                                assignee = to,
                                causalParentID = contract.parentTaskID,
                                hopCount = admissionResult.hopCount,
                                visitedAgents = admissionResult.visitedAgents,
                            )
                            scheduler.enqueue(scheduled)
                            taskGraph.updateStatus(contract.id, TaskStatus.assigned)
                            taskGraph.updateStatus(contract.id, TaskStatus.queued)
                            emitTaskCreated(contract)
                            emitTaskAssigned(contract)
                            emitTaskQueued(scheduled)
                            runBlocking { runScheduledTasks(maxExecutions = 1) }
                            awaitSchedulerResult(contract.id)
                                ?: "error: delegation task ${contract.id} did not complete"
                        }
                    }
                }
            }
        }

    fun attach(name: String, workspace: String? = null): String = engine.attach(name, workspace)

    fun detach(name: String): String = engine.detach(name)

    fun enableAutomaticPermissionReview(model: String? = null): String = engine.enableAutomaticPermissionReview(model)

    fun disableAutomaticPermissionReview(): String = engine.disableAutomaticPermissionReview()

    fun clear() = engine.clear()

    fun tick(maxExecutions: Int = 1): Int = runBlocking {
        runScheduledTasks(maxExecutions)
    }

    private suspend fun runScheduledTasks(maxExecutions: Int): Int {
        var executed = 0
        var remaining = maxExecutions
        while (remaining > 0 && scheduler.queuedTasks().isNotEmpty()) {
            val task = scheduler.runNext() ?: break
            val contract = task.contract

            scheduler.recordStarted(task)
            taskGraph.updateStatus(task.contract.id, TaskStatus.running)
            emitTaskStarted(task.contract.id, contract.assignee)
            val sender = task.issuer ?: "unknown"
            val replyTarget = scheduledReplyTargets.remove(task.contract.id)
            val result = runCatching {
                if (replyTarget != null) {
                    engine.askTaskAsync(
                        from = sender,
                        to = contract.assignee,
                        question = task.input,
                        taskID = contract.id,
                    )
                } else {
                    engine.askAsync(
                        from = sender,
                        to = contract.assignee,
                        question = task.input,
                        userGoal = "cowork_task",
                    )
                }
            }.getOrElse { ex -> "task execution failed: ${ex.message ?: "unknown error"}" }

            if (result.startsWith("the reply was blocked by the mediator") ||
                result.startsWith("tool blocked:") ||
                result.startsWith("self-targeted")
            ) {
                taskGraph.updateStatus(task.contract.id, TaskStatus.failed)
                scheduler.recordFailed(task, result)
                emitTaskFailed(task.contract.id, task.assignee, result)
            } else {
                taskGraph.updateStatus(task.contract.id, TaskStatus.completed)
                scheduler.recordCompleted(task, result)
                emitTaskCompleted(task.contract.id, task.assignee, result)
                if (replyTarget != null) {
                    messageBus.deliver(task.assignee, replyTarget, result)
                }
            }
            executed += 1
            remaining -= 1
        }
        return executed
    }

    private suspend fun enqueueAsk(
        from: String,
        to: String,
        question: String,
    ): String {
        val normalizedFrom = if (from.isBlank()) null else from
        val contract = TaskContract(
            assignee = to,
            issuer = normalizedFrom,
            objective = question,
            roleHint = "cowork",
            expectedDeliverable = "response",
        )

        when (val admission = taskGraph.validateAddTask(contract)) {
            is TaskGraphAdmissionFailure -> {
                emitTaskRejected(
                    contract = contract,
                    requester = normalizedFrom,
                    assignee = to,
                    objective = question,
                    reason = admission.violation.message,
                    violationKind = admission.violation.kind.name
                )
                return "task rejected: ${admission.violation.message}"
            }

            is TaskGraphAdmissionSuccess -> {
                val addResult = taskGraph.addRootTask(contract)
                when (addResult) {
                    is TaskGraphAdmissionFailure -> {
                        emitTaskRejected(
                            contract = contract,
                            requester = normalizedFrom,
                            assignee = to,
                            objective = question,
                            reason = addResult.violation.message,
                            violationKind = addResult.violation.kind.name
                        )
                        return "task rejected: ${addResult.violation.message}"
                    }

                    is TaskGraphAdmissionSuccess -> {
                        val admissionResult = addResult.admission
                        val scheduled = ScheduledTask(
                            contract = contract,
                            input = "[task ${contract.id}] ${contract.objective}",
                            rootTaskID = admissionResult.rootTaskID,
                            parentTaskID = contract.parentTaskID,
                            issuer = normalizedFrom,
                            assignee = to,
                            causalParentID = contract.parentTaskID,
                            hopCount = admissionResult.hopCount,
                            visitedAgents = admissionResult.visitedAgents,
                        )
                        scheduler.enqueue(scheduled)
                        taskGraph.updateStatus(contract.id, TaskStatus.assigned)
                        taskGraph.updateStatus(contract.id, TaskStatus.queued)
                        scheduledReplyTargets[contract.id] = normalizedFrom ?: "unknown"
                        emitTaskCreated(contract)
                        emitTaskAssigned(contract)
                        emitTaskQueued(scheduled)
                        runScheduledTasks(maxExecutions = 1)
                        return awaitSchedulerResult(contract.id) ?: "error: ask task ${contract.id} did not complete"
                    }
                }
            }
        }
    }

    private suspend fun awaitSchedulerResult(taskID: String): String? {
        while (true) {
            val record = scheduler.recordFor(taskID)
            when (record?.status) {
                TaskStatus.completed -> return record.result
                TaskStatus.failed -> return "error: ${record.error ?: "unknown error"}"
                else -> {
                    // continue
                }
            }

            if (runScheduledTasks(maxExecutions = 1) == 0) {
                return when (val finalRecord = scheduler.recordFor(taskID)?.status) {
                    TaskStatus.completed -> scheduler.recordFor(taskID)?.result
                    TaskStatus.failed -> "error: ${scheduler.recordFor(taskID)?.error ?: "task failed"}"
                    else -> null
                }
            }
        }
    }

    private suspend fun emitTaskCreated(contract: TaskContract) {
        eventSink.appendAsync(
            ConversationEventKinds.TaskCreated,
            mapOf(
                "contract" to contract.id,
                "assignee" to contract.assignee,
                "issuer" to (contract.issuer ?: ""),
                "objective" to contract.objective,
            )
        )
    }

    private suspend fun emitTaskAssigned(contract: TaskContract) {
        eventSink.appendAsync(
            ConversationEventKinds.TaskAssigned,
            mapOf(
                "contract" to contract.id,
                "assignee" to contract.assignee,
                "issuer" to (contract.issuer ?: ""),
            )
        )
    }

    private suspend fun emitTaskQueued(task: ScheduledTask) {
        eventSink.appendAsync(
            ConversationEventKinds.TaskQueued,
            mapOf(
                "contract" to task.contract.id,
                "assignee" to task.assignee,
                "issuer" to (task.issuer ?: ""),
                "root_task_id" to task.rootTaskID,
                "parent_task_id" to (task.parentTaskID ?: ""),
                "hop_count" to task.hopCount,
                "visited_agents" to task.visitedAgents,
            )
        )
    }

    fun queuedTasks(): List<ScheduledTask> = scheduler.queuedTasks()

    fun executionRecord(taskID: String): ExecutionRecord? = scheduler.recordFor(taskID)

    fun mailbox(agent: String): AgentMailbox = scheduler.mailboxFor(agent)

    private suspend fun emitTaskStarted(taskId: String, agent: String) {
        eventSink.appendAsync(
            ConversationEventKinds.TaskStarted,
            mapOf(
                "task_id" to taskId,
                "agent" to agent,
            )
        )
    }

    private suspend fun emitTaskCompleted(taskId: String, agent: String, result: String) {
        eventSink.appendAsync(
            ConversationEventKinds.TaskCompleted,
            mapOf(
                "task_id" to taskId,
                "agent" to agent,
                "result" to result,
            )
        )
    }

    private suspend fun emitTaskFailed(taskId: String, agent: String, error: String) {
        eventSink.appendAsync(
            ConversationEventKinds.TaskFailed,
            mapOf(
                "task_id" to taskId,
                "agent" to agent,
                "error" to error,
            )
        )
    }

    private suspend fun emitTaskRejected(
        contract: TaskContract?,
        requester: String?,
        assignee: String,
        objective: String,
        reason: String,
        violationKind: String?,
    ) {
        eventSink.appendAsync(
            ConversationEventKinds.TaskRejected,
            buildMap {
                if (contract != null) put("contract", contract.id)
                if (!requester.isNullOrBlank()) put("requester", requester)
                put("assignee", assignee)
                put("objective", objective)
                put("reason", reason)
                if (!violationKind.isNullOrBlank()) put("violation_kind", violationKind)
            }
        )
    }
}
