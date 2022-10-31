package chat.paramvr;

enum class DataType(val id: Short) {
        INT(1),
        FLOAT(2),
        BOOL(3);

    fun defaultType(): Short {
        return when (this) {
            INT -> ParameterType.LOV.id
            FLOAT -> ParameterType.SLIDER.id
            BOOL -> ParameterType.TOGGLE.id
        }
    }

    companion object {
        fun typeOf(obj: Any): DataType? {
            return when (obj) {
                is Int -> INT
                is Float -> FLOAT
                is Boolean -> BOOL
                else -> null
            }
        }
    }
}

enum class ParameterType(val id: Short) {
    LOV(1),     // Int or Float
    TOGGLE(2),  // Boolean only
    SLIDER(3);  // Float only
}