package com.intatis.shared

import com.intatis.shared.protocol.AgentID
import com.intatis.shared.protocol.MessageID
import com.intatis.shared.protocol.TaskContract
import com.intatis.shared.protocol.TaskID
import com.intatis.shared.protocol.TaskStatus

data class ScheduledTask(
    val contract: TaskContract,
    val input: String,
    val rootTaskID: TaskID? = null,
    val parentTaskID: TaskID? = null,
    val issuer: AgentID? = null,
    val assignee: AgentID,
    val causalParentID: TaskID? = null,
    val hopCount: Int = 0,
    val visitedAgents: List<AgentID> = emptyList(),
)

data class ExecutionRecord(
    val taskID: TaskID,
    val assignee: AgentID,
    val status: TaskStatus,
    val result: String? = null,
    val error: String? = null,
    val rootTaskID: TaskID? = null,
    val parentTaskID: TaskID? = null,
    val hopCount: Int,
    val visitedAgents: List<AgentID>,
)

data class AgentMailbox(
    val pendingMessages: MutableList<MessageID> = mutableListOf(),
    val pendingTasks: MutableList<TaskID> = mutableListOf(),
    val completedResults: MutableList<ExecutionRecord> = mutableListOf(),
)

class AgentScheduler {
    private val queue = mutableListOf<ScheduledTask>()
    private val records = mutableMapOf<TaskID, ExecutionRecord>()
    private val mailboxes = mutableMapOf<AgentID, AgentMailbox>()

    fun enqueue(task: ScheduledTask): TaskID {
        queue.add(task)
        records[task.contract.id] = ExecutionRecord(
            taskID = task.contract.id,
            assignee = task.assignee,
            status = TaskStatus.queued,
            rootTaskID = task.rootTaskID,
            parentTaskID = task.parentTaskID,
            hopCount = task.hopCount,
            visitedAgents = task.visitedAgents,
        )

        val mailbox = mailboxes.getOrPut(task.assignee) { AgentMailbox() }
        mailbox.pendingTasks.add(task.contract.id)
        return task.contract.id
    }

    fun runNext(): ScheduledTask? =
        if (queue.isNotEmpty()) queue.removeFirstOrNull() else null

    fun runUntilIdle(): List<ScheduledTask> {
        val drained = mutableListOf<ScheduledTask>()
        while (true) {
            val task = runNext() ?: break
            drained.add(task)
        }
        return drained
    }

    fun awaitResult(taskID: TaskID): ExecutionRecord? =
        records[taskID]

    fun recordStarted(task: ScheduledTask) {
        records[task.contract.id] = ExecutionRecord(
            taskID = task.contract.id,
            assignee = task.assignee,
            status = TaskStatus.running,
            rootTaskID = task.rootTaskID,
            parentTaskID = task.parentTaskID,
            hopCount = task.hopCount,
            visitedAgents = task.visitedAgents,
        )
        mailboxes[task.assignee]?.pendingTasks?.removeAll { it == task.contract.id }
    }

    fun recordCompleted(task: ScheduledTask, result: String) {
        val completed = ExecutionRecord(
            taskID = task.contract.id,
            assignee = task.assignee,
            status = TaskStatus.completed,
            result = result,
            rootTaskID = task.rootTaskID,
            parentTaskID = task.parentTaskID,
            hopCount = task.hopCount,
            visitedAgents = task.visitedAgents,
        )
        records[task.contract.id] = completed
        mailboxes[task.assignee]?.completedResults?.add(completed)
    }

    fun recordFailed(task: ScheduledTask, error: String) {
        val failed = ExecutionRecord(
            taskID = task.contract.id,
            assignee = task.assignee,
            status = TaskStatus.failed,
            error = error,
            rootTaskID = task.rootTaskID,
            parentTaskID = task.parentTaskID,
            hopCount = task.hopCount,
            visitedAgents = task.visitedAgents,
        )
        records[task.contract.id] = failed
        mailboxes[task.assignee]?.completedResults?.add(failed)
    }

    fun queuedTasks(): List<ScheduledTask> = queue.toList()

    fun queuedTaskCount(): Int = queue.size

    fun mailboxFor(agentID: AgentID): AgentMailbox =
        mailboxes[agentID]?.let { mailbox ->
            AgentMailbox(
                pendingMessages = mailbox.pendingMessages.toMutableList(),
                pendingTasks = mailbox.pendingTasks.toMutableList(),
                completedResults = mailbox.completedResults.toMutableList(),
            )
        } ?: AgentMailbox()

    fun recordFor(taskID: TaskID): ExecutionRecord? =
        records[taskID]
}
