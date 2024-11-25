package chat.paramvr.http

import chat.paramvr.VrcParametersClient.logger
import chat.paramvr.oscquery.ServiceDataWatcher
import com.google.gson.Gson
import com.google.gson.JsonObject
import io.ktor.client.*
import io.ktor.client.engine.okhttp.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*

object OscQueryHttpClient {

    private val client = HttpClient(OkHttp)
    private val gson = Gson()

    suspend fun isReachable(): Boolean {
        return try {
            val serviceData = ServiceDataWatcher.waitForData()
            val resp = client.get("http://127.0.0.1:${serviceData.oscQueryPort}?HOST_INFO")
            val root = gson.fromJson(resp.bodyAsText(), JsonObject::class.java)
            logger.info("Checking if VRC OSCQuery service is reachable. Response = " + resp.status)
            resp.status == HttpStatusCode.OK && root.get("NAME").asString.startsWith("VRChat-Client")
        } catch (ex: Exception) {
            logger.warn("VRC OSCQuery service not reachable", ex)
            false
        }
    }

    suspend fun getAvatarId(): String? {
        return try {

            val serviceData = ServiceDataWatcher.waitForData()
            val resp = client.get("http://127.0.0.1:${serviceData.oscQueryPort}/avatar").bodyAsText()
            val root = gson.fromJson(resp, JsonObject::class.java)

            val avtrId = root.getAsJsonObject("CONTENTS")
                .getAsJsonObject("change")
                .getAsJsonArray("VALUE")[0].asString

            logger.info("Got avatar id from VRC OSCQuery service = $avtrId")
            avtrId

        } catch (ex: Exception) {
            logger.error("Error getting avatar id from VRC OSCQuery service.", ex)
            null
        }
    }
}