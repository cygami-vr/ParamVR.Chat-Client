package chat.paramvr.http

import chat.paramvr.VrcParametersClient.logger
import chat.paramvr.cfg
import com.google.gson.Gson
import com.google.gson.JsonElement
import io.ktor.client.*
import io.ktor.client.engine.okhttp.*
import io.ktor.client.request.*
import io.ktor.client.request.forms.*
import io.ktor.client.statement.*
import io.ktor.http.*
import kotlinx.coroutines.runBlocking
import okhttp3.internal.closeQuietly
import java.nio.charset.StandardCharsets
import java.util.*
import javax.swing.JOptionPane

object BasicHttpClient {

    private var client: HttpClient? = null
    private var useSsl: Boolean? = null
    private val gson = Gson()

    init {
        init()
    }

    fun init() {
        client?.let {
            it.closeQuietly()
        }

        val cfgHost = cfg.getHost()
        useSsl = cfgHost != "localhost" && cfgHost != "127.0.0.1"

        client = HttpClient(OkHttp) {
            if (useSsl!!) {
                engine {
                    config {
                        sslSocketFactory(SslSettings.getSslContext()!!.socketFactory, SslSettings.getTrustManager())
                    }
                }
            }
        }
    }

    fun getAuthorization(): String {
        val targetUser = cfg.getTargetUser()
        val listenKey = cfg.getListenKey()
        val utf8Encoded = "$targetUser:$listenKey".toByteArray(StandardCharsets.UTF_8)
        val base64Encoded = Base64.getEncoder().encodeToString(utf8Encoded)
        return "Basic $base64Encoded"
    }

    private fun buildUrl(reqBuilder: HttpRequestBuilder, path: String) {
        reqBuilder.url {
            protocol = if (useSsl!!) URLProtocol.HTTPS else URLProtocol.HTTP
            host = cfg.getHost()
            port = cfg.getPort()
            this.path(path)
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

    private suspend fun tryPost(path: String, json: JsonElement?): HttpResponse? {
        logger.info("POST: $json to $path")
        return try {
            client!!.post {
                headers.append("Authorization", getAuthorization())
                method = HttpMethod.Post
                buildUrl(this, path)
                json?.let {
                    headers.append("Content-Type", "application/json")
                    setBody(gson.toJson(it))
                }
            }
        } catch (ex: Exception) {
            logger.error("Error submitting http post", ex)
            null
        }
    }

    fun submitForm(path: String, fileName: String, data: ByteArray) {
        runBlocking {
            trySubmitForm(path, fileName, data)?.let {
                if (!it.status.isSuccess()) {
                    JOptionPane.showMessageDialog(null,
                        "Request to $path failed\n" +
                                "Response was ${it.status.value} ${it.status.description}")
                }
            }
        }
    }

    private suspend fun trySubmitForm(path: String, fileName: String, data: ByteArray): HttpResponse? {
        return try {
            client!!.submitFormWithBinaryData(
                formData = formData {
                    append("log", data, Headers.build {
                        append(HttpHeaders.ContentType, "text/plain")
                        append(HttpHeaders.ContentDisposition, "filename=\"$fileName\"")
                    })
                }
            ) {
                method = HttpMethod.Post
                headers.append("Authorization", getAuthorization())
                buildUrl(this, path)
            }
        } catch (ex: Exception) {
            logger.error("Error submitting http form", ex)
            null
        }
    }
}