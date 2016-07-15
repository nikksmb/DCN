using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.PipeConnection;

namespace DistributedComputingNetwork.DCN
{
    /// <summary>
    /// Using by programmers to calculate parallel long operations via distributed computing network.
    /// </summary>
    public static class Dcn
    {
        //create async methods
        //private methods for check connection

        private static LibraryConnector libConnection;
        private static bool initialized;

        static Dcn()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            libConnection = new LibraryConnector();
            libConnection.ConnectAsync();

            Assembly assm = Assembly.GetEntryAssembly();
            //Assembly.GetExecutingAssembly();
            //Assembly.GetCallingAssembly();
            //Assembly.GetEntryAssembly();
            string pathToAssm = assm.Location;
            byte[] assmBytes = File.ReadAllBytes(pathToAssm);
            AppDomain.CurrentDomain.Load(assmBytes);
        }

        public static TResponse ParametrizedDelegate<TRequest,TResponse>(Func<TRequest,TResponse> func, TRequest request )
        {
            if (libConnection.ConnectionState)
            {
                CalculationPackage package = new CalculationPackage
                {
                    Func = func,
                    Argument = request
                };
                object result = libConnection.GetAnswer(InformationType.DelegateAndImmediateExecute, package);
                if (result is TResponse)
                {
                    return (TResponse)result;
                }
                else
                {
                    if (result is Exception)
                    {
                        throw new Exception(((Exception)result).Message, (Exception)result);
                    }
                    if (result == null)
                    {
                        return func(request);
                    }
                    //possible error messages were returned
                }
            }
            return func(request);
        }

        public static TResponse ParameterlessDelegate<TResponse>(Func<TResponse> func)
        {
            if (libConnection.ConnectionState)
            {
                //transform to Func<object>
                //Func<object> transformedFunc = () => func();

                object result = libConnection.GetAnswer(InformationType.DelegateAndImmediateExecute, func);
                if (result is TResponse)
                {
                    return (TResponse)result;
                }
                else
                {
                    if (result is Exception)
                    {
                        throw new Exception(((Exception) result).Message, (Exception) result);
                    }
                    if (result == null)
                    {
                        return func();
                    }
                    //possible error messages were returned
                }
            }
            return func();
        }

        /*     public static TResponse ParametrizedExpression<TRequest, TResponse>(Expression<Func<TRequest, TResponse>> expression, TRequest request)
             {
                 if (libConnection.ConnectionState)
                 {
                     return ParameterlessDelegate(() => expression.Compile()(request));

                  /*   object result = libConnection.GetAnswer(InformationType.ExpressionAndImmediateExecute, );
                     if (result is TResponse)
                     {
                         return (TResponse)result;
                     }
                     else
                     {
                         //possible error messages were returned
                     }
                 }

                 //send it in application
                 return expression.Compile()(request);
             }*/
    }
}
