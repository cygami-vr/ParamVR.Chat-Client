package chat.paramvr.http

import chat.paramvr.VrcParametersClient.logger
import com.google.gson.JsonElement
import io.ktor.client.statement.*
import io.ktor.http.*
import kotlinx.coroutines.runBlocking
import javax.swing.JOptionPane

abstract class BasicHttpClient {

    protected abstract suspend fun doPost(path: String, json: JsonElement?): HttpResponse?
    private suspend fun tryPost(path: String, json: JsonElement?): HttpResponse? {
        logger.info("POST: $json to $path")
        return try {
            doPost(path, json)
        } catch (ex: Exception) {
            logger.error("Error submitting http post", ex)
            null
        }
    }

    fun post(path: String, json: JsonElement?) {
        runBlocking {
            tryPost(path, json)?.let {
                if (!it.status.isSuccess()) {
                    JOptionPane.showMessageDialog(null,
                        "Request to $path failed\n" +
                                "Response was ${it.status.value} ${it.status.description}")
                }
            }
        }
    }
}