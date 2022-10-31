package chat.paramvr

import chat.paramvr.VrcParametersClient.logger
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.Paths
import java.util.Properties

abstract class Config(private var path: Path) {

    protected val props = Properties()

    init {
        load()
        populate()
        save()
    }

    fun setPath(path: Path) {
        this.path = path
        logger.info("Overriding default path to $path")
        load()
        populate()
        save()
    }

    fun reset() {
        Files.delete(getPath())
        getPath() // cause the file to be recreated
        props.clear()
        populate()
        save()
    }

    private fun getPath(): Path {

        val resolved = Paths.get(AppData.paramVR()).resolve(path)
        logger.info("Resolved = $resolved")

        Files.createDirectories(resolved.parent)
        if (!Files.exists(resolved)) {
            Files.createFile(resolved)
        }

        return resolved
    }

    private fun load() {
        try {
            Files.newInputStream(getPath()).use { props.load(it) }
        } catch (ex: IOException) {
            logger.warn("Error loading config", ex)
        }
    }

    protected abstract fun populate()

    fun getInt(prop: String) = getString(prop)?.toInt()

    fun getString(prop: String): String? = props.getProperty(prop)

    protected fun populate(prop: String, defaultValue: String, test: (prop: String) -> Boolean) {
        val obj = props.computeIfAbsent(prop) { defaultValue }
        if (!test(obj.toString()))
            props.setProperty(prop, defaultValue)
    }

    fun save() {
        try {
            Files.newOutputStream(getPath()).use { props.store(it, null) }
        } catch (ex: IOException) {
            logger.warn("Error saving config", ex)
        }
    }
}

fun String.testInt(): Boolean {
    try {
        this.toInt()
    } catch (ex: NumberFormatException) {
        logger.warn("Error parsing $this to int")
        return false
    }
    return true
}