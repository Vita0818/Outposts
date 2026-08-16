package com.intatis.shared.protocol

data class SessionCreateParams(
    val kind: SessionKind,
    val title: String? = null
)

data class SessionResumeParams(
    val session: SessionID,
    val fromSeq: Int = 0
)

data class MessageSendParams(
    val session: SessionID,
    val text: String,
    val attachments: List<ArtifactID>? = null,
    val to: AgentID? = null
)

data class PermissionRespondParams(
    val session: SessionID,
    val requestId: RequestID,
    val decision: PermissionDecision
)

data class AgentAttachParams(
    val session: SessionID,
    val name: AgentID,
    val path: String,
    val model: ModelID? = null
)

data class ProfileSetParams(
    val session: SessionID,
    val agent: AgentID,
    val mode: String
)

sealed interface Command {
    val method: Method

    enum class Method(val value: String) {
        sessionCreate("session.create"),
        sessionResume("session.resume"),
        sessionList("session.list"),
        messageSend("message.send"),
        permissionRespond("permission.respond"),
        agentAttach("agent.attach"),
        profileSet("profile.set"),
    }
}

data class SessionCreateCommand(val params: SessionCreateParams) : Command {
    override val method = Command.Method.sessionCreate
}

data class SessionResumeCommand(val params: SessionResumeParams) : Command {
    override val method = Command.Method.sessionResume
}

object SessionListCommand : Command {
    override val method = Command.Method.sessionList
}

data class MessageSendCommand(val params: MessageSendParams) : Command {
    override val method = Command.Method.messageSend
}

data class PermissionRespondCommand(val params: PermissionRespondParams) : Command {
    override val method = Command.Method.permissionRespond
}

data class AgentAttachCommand(val params: AgentAttachParams) : Command {
    override val method = Command.Method.agentAttach
}

data class ProfileSetCommand(val params: ProfileSetParams) : Command {
    override val method = Command.Method.profileSet
}
