namespace fingerprint_service.Models
{
    //En la base de datos se almacenan en números enteros
    public enum Fingers
    {
        /// <summary>
        /// Pulgar derecho (Right thumb) -0
        /// </summary>
        RIGHT_THUMB,

        /// <summary>
        /// Índice derecho (Right index finger) -1
        /// </summary>
        RIGHT_INDEX,

        /// <summary>
        /// Medio derecho (Right middle finger) -2
        /// </summary>
        RIGHT_MIDDLE,

        /// <summary>
        /// Anular derecho (Right ring finger) -3
        /// </summary>
        RIGHT_RING,

        /// <summary>
        /// Meñique derecho (Right pinky finger) -4
        /// </summary>
        RIGHT_PINKY,

        /// <summary>
        /// Pulgar izquierdo (Left thumb) -5
        /// </summary>
        LEFT_THUMB,

        /// <summary>
        /// Índice izquierdo (Left index finger) -6
        /// </summary>
        LEFT_INDEX,

        /// <summary>
        /// Medio izquierdo (Left middle finger) -7
        /// </summary>
        LEFT_MIDDLE,

        /// <summary>
        /// Anular izquierdo (Left ring finger) -8
        /// </summary>
        LEFT_RING,

        /// <summary>
        /// Meñique izquierdo (Left pinky finger) -9
        /// </summary>
        LEFT_PINKY
    }
}