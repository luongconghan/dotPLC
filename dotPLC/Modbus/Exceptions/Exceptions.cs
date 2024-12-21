using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace dotPLC.Modbus.Exceptions
{
    /// <summary>
    /// Exception to be thrown if Modbus Server returns error code "Function Code not executed (0x04)"
    /// </summary>
    public class ModbusException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class.
        /// </summary>
        public ModbusException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ModbusException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ModbusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected ModbusException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    /// <summary>
    ///  The exception thrown when tcp client not opened.
    /// </summary>
    public class SocketNotOpenedException : ModbusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class.
        /// </summary>
        public SocketNotOpenedException(): base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SocketNotOpenedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SocketNotOpenedException(string message, Exception innerException): base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketNotOpenedException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected SocketNotOpenedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    ///Represents errors that occur when serial port is not opened
    /// </summary>
    public class SerialPortNotOpenedException : ModbusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortNotOpenedException"></see> class.
        /// </summary>
        public SerialPortNotOpenedException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortNotOpenedException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SerialPortNotOpenedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortNotOpenedException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SerialPortNotOpenedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortNotOpenedException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected SerialPortNotOpenedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when connection to the server modbus failed.
    /// </summary>
    public class ConnectionException : ModbusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class.
        /// </summary>
        public ConnectionException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected ConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception to be thrown if Modbus Server returns error code "Function code not supported"
    /// </summary>
    public class FunctionCodeNotSupportedException : ModbusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCodeNotSupportedException"></see> class.
        /// </summary>
        public FunctionCodeNotSupportedException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCodeNotSupportedException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public FunctionCodeNotSupportedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCodeNotSupportedException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public FunctionCodeNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCodeNotSupportedException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected FunctionCodeNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception to be thrown if Modbus Server returns error code "Size invalid"
    /// </summary>
    public class SizeInvalidException : ModbusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SizeInvalidException"></see> class.
        /// </summary>
        public SizeInvalidException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeInvalidException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SizeInvalidException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeInvalidException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SizeInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeInvalidException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected SizeInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception to be thrown if Modbus Server returns error code "starting adddress and quantity invalid"
    /// </summary>
    public class StartingAddressInvalidException : ModbusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class.
        /// </summary>
        public StartingAddressInvalidException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public StartingAddressInvalidException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public StartingAddressInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartingAddressInvalidException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected StartingAddressInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception to be thrown if CRC Check failed
    /// </summary>
    public class CRCCheckFailedException : ModbusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CRCCheckFailedException"></see> class.
        /// </summary>
        public CRCCheckFailedException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CRCCheckFailedException"></see> class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public CRCCheckFailedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CRCCheckFailedException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public CRCCheckFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CRCCheckFailedException"></see> class using the specified serialization data and context.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to be used for deserialization.</param>
        /// <param name="context">The destination to be used for deserialization.</param>
        protected CRCCheckFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
