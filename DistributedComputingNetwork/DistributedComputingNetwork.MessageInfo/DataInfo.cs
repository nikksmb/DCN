using System;
using System.Runtime.InteropServices;

namespace DistributedComputingNetwork.MessageInfo
{
    public struct DataInfo
    {
        public InformationType TypeOfMessage;
        public Int32 Size;

        public DataInfo(byte[] bytes)
        {
            if (bytes?.Length == Marshal.SizeOf(typeof(DataInfo)))
            {
                TypeOfMessage = (InformationType)bytes[0];
                Size = BitConverter.ToInt32(bytes,1);
                return;
            }
            throw new InvalidOperationException("Wrong size of bytes");
        }

        public byte[] ToBytes()
        {
            byte[] result = new byte[Marshal.SizeOf(typeof(DataInfo))];
            result[0] = (byte)TypeOfMessage;
            byte[] size = BitConverter.GetBytes(Size);
            result[1] = size[0];
            result[2] = size[1];
            result[3] = size[2];
            result[4] = size[3];
            return result;
        }

        public static InformationType? Response(InformationType typeOfRequest)
        {
            switch(typeOfRequest)
            {
                case InformationType.AskNetworkCellInformation:
                    return InformationType.NetworkCellInformation;
                case InformationType.AskSettings:
                    return InformationType.NetworkSettings;
                case InformationType.Assembly:
                    return null;
                case InformationType.CalculationResult:
                    return null;
                case InformationType.DelegateAndImmediateExecute:
                    return InformationType.CalculationResult;
                case InformationType.DelegateParameters:
                    return InformationType.CalculationResult;
                case InformationType.QueueFlush:
                    return null;
                case InformationType.LostConnection:
                    return null;
                case InformationType.ExpressionParameters:
                    return InformationType.CalculationResult;
                case InformationType.ExpressionAndImmediateExecute:
                    return InformationType.CalculationResult;
                case InformationType.NullInformation:
                    return null;
                case InformationType.StoreDelegate:
                    return null;
                case InformationType.StoreExpression:
                    return null;
                case InformationType.TextMessage:
                    return null;
                case InformationType.NetworkSettings:
                    return null;
                case InformationType.NetworkCellInformation:
                    return null;
                default:
                    return null;
            }
        }
    }

    public enum InformationType
    {
        /// <summary>
        /// Without notification of getting information
        /// </summary>
        NullInformation = 0,
        /// <summary>
        /// Request for network speed, latency and ip-address. Answer should be sent immediately
        /// </summary>
        AskNetworkCellInformation = 1,
        /// <summary>
        /// Information about connection
        /// </summary>
        NetworkCellInformation = 2,
        /// <summary>
        /// Request for settings both library and application
        /// </summary>
        AskSettings = 3,
        /// <summary>
        /// Settings of application
        /// </summary>
        NetworkSettings = 4,
        /// <summary>
        /// Settings of library
        /// </summary>
        TextMessage = 6,
        ExpressionAndImmediateExecute = 7,
        CalculationResult = 8,
        DelegateAndImmediateExecute = 9,
        StoreExpression = 10,
        StoreDelegate = 11,
        ExpressionParameters = 12,
        DelegateParameters = 13,
        /// <summary>
        /// From library to application in order to execute delegate
        /// </summary>
        Assembly = 14,
        /// <summary>
        /// Use only in pipes; after decoding this message data flow in current pipe will be reversed
        /// </summary>
        QueueFlush = 15,
        /// <summary>
        /// Indicates that connection is lost as substitude to null answer
        /// </summary>
        LostConnection = 16,
        /// <summary>
        /// Used to send information about length of queue in DataInfo.Size
        /// </summary>
        QueueLength = 17,
        /// <summary>
        /// Used by loggers
        /// </summary>
        LogInfo = 18
    }

}
