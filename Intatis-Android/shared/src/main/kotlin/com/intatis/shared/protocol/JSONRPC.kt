package com.intatis.shared.protocol

object JSONRPC {
    const val version = "2.0"
    const val eventMethod = "event"
}

data class JSONRPCErrorObject(
    val code: Int,
    val message: String
) {
    companion object {
        val parseError = JSONRPCErrorObject(code = -32700, message = "Parse error")
        val invalidRequest = JSONRPCErrorObject(code = -32600, message = "Invalid request")
        val methodNotFound = JSONRPCErrorObject(code = -32601, message = "Method not found")
        val invalidParams = JSONRPCErrorObject(code = -32602, message = "Invalid params")
        val internalError = JSONRPCErrorObject(code = -32603, message = "Internal error")
    }
}
