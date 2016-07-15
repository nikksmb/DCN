using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DistributedComputingNetwork.DCN;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.NetworkDispatcher;
using DistributedComputingNetwork.PipeConnection;
using DistributedComputingNetwork.SubsystemInterfaces;
using Serialize.Linq.Serializers;

namespace DistributedComputingNetwork.CalculationCore
{
    public class CalculationManager:ISubsystem
    {
        /// <summary>
        /// This subsystem is not going to send any request
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void PutAnswer(InformationType type, object data)
        {
            if (type != InformationType.Assembly)
                return;
            byte[] assembly = (byte[])data;
            Dispatcher.Logger.PutAnswer(InformationType.LogInfo,"start loading assembly");
            Assembly assm = AppDomain.CurrentDomain.Load(assembly);
            Dispatcher.Logger.PutAnswer(InformationType.LogInfo, $"end load {assm.FullName}");
        }

        public object GetAnswer(InformationType requestType, object requestData)
        {
            //NetworkPackage -> PipePackage -> RequestPackage
            PipePackage pipePackage = (PipePackage)requestData;
            RequestPackage requestPackage = (RequestPackage) pipePackage.Data;
            object item = requestPackage.Data;
            object result = null;
            L1:
            try
            {
                switch (requestType)
                {
                    case InformationType.DelegateAndImmediateExecute:
                        Dispatcher.Logger.PutAnswer(InformationType.LogInfo, "start executing delegate");
                        result = DelegateAndImmediateExecute(item);
                        Dispatcher.Logger.PutAnswer(InformationType.LogInfo, "end executing delegate");
                        break;
                    case InformationType.DelegateParameters:
                        break;
                    case InformationType.ExpressionAndImmediateExecute:
                        Dispatcher.Logger.PutAnswer(InformationType.LogInfo, "start executing expression");
                        result = ExpressionAndImmediateExecute(item);
                        Dispatcher.Logger.PutAnswer(InformationType.LogInfo, "end executing expression");
                        break;
                    case InformationType.ExpressionParameters:
                        break;
                    case InformationType.StoreDelegate:
                        break;
                    case InformationType.StoreExpression:
                        break;
                    default:
                        return null;
                }
            }
            catch(AggregateException)
            {
                Thread.Sleep(100);
                Dispatcher.Logger.PutAnswer(InformationType.LogInfo, "failed to deserialize delegate");
                goto L1;
            }
            catch(Exception e)
            {
                result = e;
            }
            requestPackage.Data = result;
            pipePackage.Data = requestPackage;
            return pipePackage;
        }

        private object DelegateAndImmediateExecute(object func)
        {
            dynamic res;
            if (func is CalculationPackage)
            {
                CalculationPackage package = (CalculationPackage) func;
                dynamic args = package.Argument;
                res = package.Func;
                return res(args);
            }
            res = func;
            return res();
        }

        private object ExpressionAndImmediateExecute(object expression)
        {
            //! without any reliability
            BinarySerializer binarySerializer = new BinarySerializer();
            ExpressionSerializer serializer = new ExpressionSerializer(binarySerializer);
            Expression expr = serializer.DeserializeBinary((byte[])expression);
            LambdaExpression lambda = Expression.Lambda(expr);
            Delegate d = lambda.Compile();
            return d.DynamicInvoke();
        }
    }
}
