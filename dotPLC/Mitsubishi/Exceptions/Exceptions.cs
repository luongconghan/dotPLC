using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace dotPLC.Mitsubishi.Exceptions
{
    /// <summary>
    /// Represents errors that occur during Mitsubishi Communication execution.
    /// </summary>
    public class MitsubishiException : ArgumentException
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="MitsubishiException"></see> class.
        /// </summary>
        public MitsubishiException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MitsubishiException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MitsubishiException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MitsubishiException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public MitsubishiException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MitsubishiException"></see> class with a specified error message and the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public MitsubishiException(string message, string paramName) : base(message, paramName)
        {

        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="MitsubishiException"></see> class with a specified error message, the parameter name, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="innerException"> The exception that is the cause of the current exception. If the innerException parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public MitsubishiException(string message, string paramName, Exception innerException) : base(message, paramName, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MitsubishiException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        public MitsubishiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
	/// The exception that is thrown when connection to the server failed.
	/// </summary>
	public class ConnectionException : MitsubishiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class.
        /// </summary>
        public ConnectionException()
         : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected ConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when device label is invalid.
    /// </summary>
    public class InvalidDeviceLabelNameException : MitsubishiException
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDeviceLabelNameException"></see> class.
        /// </summary>
        public InvalidDeviceLabelNameException()
         : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDeviceLabelNameException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InvalidDeviceLabelNameException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDeviceLabelNameException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public InvalidDeviceLabelNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDeviceLabelNameException"></see> class with a specified error message and the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public InvalidDeviceLabelNameException(string message, string paramName) : base(message, paramName)
        {

        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="InvalidDeviceLabelNameException"></see> class with a specified error message, the parameter name, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="innerException"> The exception that is the cause of the current exception. If the innerException parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public InvalidDeviceLabelNameException(string message, string paramName, Exception innerException) : base(message, paramName, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDeviceLabelNameException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected InvalidDeviceLabelNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///  The exception that is thrown when the address of device is outside the allowable range of mitsubishi memory.
    /// </summary>
    public class DeviceAddressOutOfRangeException : MitsubishiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddressOutOfRangeException"></see> class.
        /// </summary>
        public DeviceAddressOutOfRangeException()
         : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddressOutOfRangeException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DeviceAddressOutOfRangeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddressOutOfRangeException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public DeviceAddressOutOfRangeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddressOutOfRangeException"></see> class with a specified error message and the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public DeviceAddressOutOfRangeException(string message, string paramName) : base(message, paramName)
        {

        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="DeviceAddressOutOfRangeException"></see> class with a specified error message, the parameter name, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="innerException"> The exception that is the cause of the current exception. If the innerException parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public DeviceAddressOutOfRangeException(string message, string paramName, Exception innerException) : base(message, paramName, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddressOutOfRangeException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected DeviceAddressOutOfRangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///  The exception thrown when using data type that is not compatible with the PLC.
    /// </summary>
    public class InvalidDataTypeException : MitsubishiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataTypeException"></see> class.
        /// </summary>
        public InvalidDataTypeException()
         : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataTypeException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InvalidDataTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataTypeException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public InvalidDataTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataTypeException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected InvalidDataTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///  The exception thrown when tcp client not opened.
    /// </summary>
    public class SocketNotOpenedException : MitsubishiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class.
        /// </summary>
        public SocketNotOpenedException()
         : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SocketNotOpenedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SocketNotOpenedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected SocketNotOpenedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    ///  The exception thrown when number of devices processed per communication is outside the allowable range.
    /// </summary>
    public class QuantityOutOfRangeException : MitsubishiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuantityOutOfRangeException"></see> class.
        /// </summary>
        public QuantityOutOfRangeException()
         : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantityOutOfRangeException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public QuantityOutOfRangeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantityOutOfRangeException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public QuantityOutOfRangeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantityOutOfRangeException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected QuantityOutOfRangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
   
    /// <summary>
    /// The exception thrown when number of points processed per communication is outside the allowable range.
    /// </summary>
    public class SizeOutOfRangeException : MitsubishiException
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeOutOfRangeException"></see> class.
        /// </summary>
        public SizeOutOfRangeException()
         : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeOutOfRangeException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SizeOutOfRangeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeOutOfRangeException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SizeOutOfRangeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeOutOfRangeException"></see> class with a specified error message and the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public SizeOutOfRangeException(string message, string paramName) : base(message, paramName)
        {

        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="SizeOutOfRangeException"></see> class with a specified error message, the parameter name, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="innerException"> The exception that is the cause of the current exception. If the innerException parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public SizeOutOfRangeException(string message, string paramName, Exception innerException) : base(message, paramName, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeOutOfRangeException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected SizeOutOfRangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
