package chat.paramvr.http

import chat.paramvr.VrcParametersClient.logger
import chat.paramvr.cfg
import java.io.FileInputStream
import java.nio.file.Files
import java.nio.file.Paths
import java.security.KeyStore
import javax.net.ssl.SSLContext
import javax.net.ssl.TrustManagerFactory
import javax.net.ssl.X509TrustManager

object SslSettings {
    private fun getKeyStore(): KeyStore {
        val javaHome = System.getProperty("java.home")
        logger.info("java.home = $javaHome")

        val defaultPath = "$javaHome/jre/lib/security/cacerts"
        val keyStorePath = if (!cfg.getKeyStoreFile().isNullOrEmpty()) cfg.getKeyStoreFile()
            else if (Files.exists(Paths.get(defaultPath))) defaultPath
            else "$javaHome/lib/security/cacerts"

        logger.info("Getting key store. Cfg path =  ${cfg.getKeyStoreFile()}," +
                " Actual path = $keyStorePath, Passwd = ${cfg.getKeyStorePassword()}")

        val keyStoreFile = FileInputStream(keyStorePath)
        val keyStorePassword = cfg.getKeyStorePassword()
        val keyStore: KeyStore = KeyStore.getInstance(KeyStore.getDefaultType())
        keyStore.load(keyStoreFile, keyStorePassword.toCharArray())
        return keyStore
    }

    private fun getTrustManagerFactory(): TrustManagerFactory? {
        val trustManagerFactory = TrustManagerFactory.getInstance(TrustManagerFactory.getDefaultAlgorithm())
        trustManagerFactory.init(getKeyStore())
        return trustManagerFactory
    }

    fun getSslContext(): SSLContext? {
        val sslContext = SSLContext.getInstance("TLS")
        sslContext.init(null, getTrustManagerFactory()?.trustManagers, null)
        return sslContext
    }

    fun getTrustManager(): X509TrustManager {
        return getTrustManagerFactory()?.trustManagers?.first { it is X509TrustManager } as X509TrustManager
    }
}