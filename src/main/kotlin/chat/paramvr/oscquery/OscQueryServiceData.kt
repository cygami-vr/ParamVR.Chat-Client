package chat.paramvr.oscquery

import com.google.gson.annotations.SerializedName

data class OscQueryServiceData(
    @SerializedName("OscPortIn")
    val oscPortIn: Int,
    @SerializedName("OscPortOut")
    val oscPortOut: Int,
    @SerializedName("Pid")
    val pid: Long,
    @SerializedName("OscQueryPort")
    val oscQueryPort: Int)