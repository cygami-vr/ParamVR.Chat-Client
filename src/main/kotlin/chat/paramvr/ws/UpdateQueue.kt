package chat.paramvr.ws

import chat.paramvr.VrcParametersClient.logger
import com.google.gson.JsonArray
import com.google.gson.JsonObject
import io.ktor.client.plugins.websocket.*
import kotlinx.coroutines.runBlocking
import chat.paramvr.tray.Advanced
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit

object UpdateQueue {

    private val executor = Executors.newSingleThreadScheduledExecutor()
    private val pendingUpdates = mutableMapOf<String, Any>()
    private var scheduled = false
    private val mutex = Object()

    fun enqueue(name: String, value: Any) {
        logger.debug("Scheduling update {} = {}, Pending = {}", name, value, pendingUpdates.size)
        synchronized(mutex) {
            if (!scheduled) {
                executor.schedule(this::sendUpdates, 1, TimeUnit.SECONDS)
                scheduled = true
            }
            pendingUpdates[name] = value
        }
    }

    private fun sendUpdates() {
        synchronized(mutex) {
            scheduled = false
            try {
                if (Advanced.vrcpWsStatus.isConnected()) {
                    val arr = createJsonArray()
                    if (!arr.isEmpty) {
                        pendingUpdates.clear()
                        logger.debug("Sending {} updates", arr.size())
                        send(arr)
                    }
                }
            } catch (ex: Exception) {
                logger.error("Error sending updates across websocket", ex)
            }
        }
    }

    private fun createJsonArray(): JsonArray {
        val arr = JsonArray()
        pendingUpdates.entries.forEach {
            arr.add(createJsonObject(it.key, it.value))
        }
        return arr
    }

    private fun send(arr: JsonArray) {
        runBlocking {
            WebSocketController.webSocket?.sendSerialized(arr)
        }
    }

    private fun createJsonObject(name: String, value: Any): JsonObject {
        val update = JsonObject()
        update.addProperty("name", name)
        when (value) {
            is Boolean -> update.addProperty("value", value)
            is String -> update.addProperty("value", value)
            is Number -> update.addProperty("value", value)
            else -> logger.warn("unsupported parameter value type for $name")
        }
        return update
    }
}