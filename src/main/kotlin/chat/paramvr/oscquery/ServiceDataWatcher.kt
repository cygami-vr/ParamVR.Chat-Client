package chat.paramvr.oscquery

import chat.paramvr.AppData
import chat.paramvr.VrcParametersClient.logger
import com.google.gson.Gson
import java.nio.file.*

object ServiceDataWatcher {

    private val gson = Gson()
    private var serviceData: OscQueryServiceData? = null
    private val watchService = FileSystems.getDefault().newWatchService()
    private val jsonFile = Paths.get(AppData.paramVR()).resolve("ParamVR.Chat-OSCQueryService.json")
    private var watchKey: WatchKey? = null
    private var thread: Thread? = null
    private val waitObject = Object()

    fun startThread() {
        watchKey = jsonFile.parent.register(watchService,
            StandardWatchEventKinds.ENTRY_CREATE, StandardWatchEventKinds.ENTRY_MODIFY)
        thread = Thread { pollLoop() }
        thread?.let {
            it.isDaemon = true
            it.start()
        }
    }

    private fun pollLoop() {
        while (true) {
            watchKey?.let {
                if (isJsonFileUpdated(it.pollEvents())) {
                    readServiceData()
                }
            }
            Thread.sleep(1000)
        }
    }

    private fun isJsonFileUpdated(evts: List<WatchEvent<*>>): Boolean {
        return evts.any {
            val evtPath = it.context() as Path
            return evtPath.fileName.toString() == jsonFile.fileName.toString()
        }
    }

    private fun readServiceData() {
        val json = Files.readString(jsonFile)
        val newData = gson.fromJson(json, OscQueryServiceData::class.java)
        if (newData.oscPortIn != 0 && newData.oscPortOut != 0) {

            serviceData = newData
            logger.info("New service data obtained; notifying all")
            synchronized (waitObject) {
                waitObject.notifyAll()
            }
            listeners.forEach {
                it(newData)
            }
        }
    }

    fun waitForData(): OscQueryServiceData {
        if (serviceData == null) {
            logger.info("Waiting for service data")
            synchronized (waitObject) {
                waitObject.wait()
            }
            return serviceData!!
        }
        logger.info("Service data already available")
        return serviceData!!
    }

    private val listeners = mutableListOf<(data: OscQueryServiceData) -> Unit>()

    fun registerServiceDataListener(listener: (data: OscQueryServiceData) -> Unit) {
        listeners.add(listener)
    }
}