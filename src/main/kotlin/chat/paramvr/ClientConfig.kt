package chat.paramvr

import java.nio.file.Paths

class ClientConfig : Config(Paths.get("ParamVR.Chat-Client.properties")) {

    override fun populate() {
        props.computeIfAbsent(targetUser) { "" }
        props.computeIfAbsent(listenKey) { "" }
        props.computeIfAbsent(host) { "paramvr.chat" }
        populate(port, "443") { it.testInt() }
        populate(oscOutPort, "9000") { it.testInt() }
        populate(oscInPort, "9001") { it.testInt() }
        props.computeIfAbsent(keyStoreFile) { "" }
        props.computeIfAbsent(keyStorePassword) { "changeit" }
        props.computeIfAbsent(defaultAvatar) { "" }
    }

    fun getTargetUser() = getString(targetUser)

    fun setTargetUser(targetUser: String) {
        props.setProperty(Companion.targetUser, targetUser)
        save()
    }

    fun getListenKey() = getString(listenKey)

    fun setListenKey(listenKey: String) {
        props.setProperty(Companion.listenKey, listenKey)
        save()
    }

    fun getHost() = getString(host)!!

    fun setHost(host: String) {
        props.setProperty(Companion.host, host)
        save()
    }

    fun getPort() = getInt(port)!!

    fun setPort(port: String) {
        props.setProperty(Companion.port, port)
        save()
    }

    fun getOscOutPort() = getInt(oscOutPort)!!

    fun setOscOutPort(oscOutPort: String) {
        props.setProperty(Companion.oscOutPort, oscOutPort)
        save()
    }

    fun getOscInPort() = getInt(oscInPort)!!

    fun setOscInPort(oscInPort: String) {
        props.setProperty(Companion.oscInPort, oscInPort)
        save()
    }

    fun getKeyStoreFile() = getString(keyStoreFile)

    fun setKeyStoreFile(keyStoreFile: String) {
        props.setProperty(Companion.keyStoreFile, keyStoreFile)
        save()
    }

    fun getKeyStorePassword() = getString(keyStorePassword)!!

    fun setKeyStorePassword(keyStorePassword: String) {
        props.setProperty(Companion.keyStorePassword, keyStorePassword)
        save()
    }

    fun getDefaultAvatar() = getString(defaultAvatar)

    fun setDefaultAvatar(defaultAvatar: String) {
        props.setProperty(Companion.defaultAvatar, defaultAvatar)
        save()
    }

    companion object {
        private const val targetUser = "targetUser"
        private const val listenKey = "listenKey"
        private const val host = "host"
        private const val port = "port"
        private const val oscOutPort = "oscOutPort"
        private const val oscInPort = "oscInPort"
        private const val keyStoreFile = "keyStoreFile"
        private const val keyStorePassword = "keystorePassword"
        private const val defaultAvatar = "defaultAvatar"
    }
}