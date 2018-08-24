﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.DependencyInjection
{
    public class ConcurrentProcessAttribute : InterceptorAttribute
    {
        public ConcurrentProcessAttribute(int retryTimes = 50)
        {
            RetryTimes = retryTimes;
        }
        public int RetryTimes { get; set; }
        public override Task<object> ProcessAsync(Func<Task<object>> funcAsync,
                                                  IObjectProvider objectProvider,
                                                  Type targetType,
                                                  object invocationTarget,
                                                  MethodInfo method,
                                                  MethodInfo methodInvocationTarget)
        {
            var concurrencyProcessor = objectProvider.GetService<IConcurrencyProcessor>();
            return concurrencyProcessor.ProcessAsync(funcAsync, RetryTimes);
        }

        public override object Process(Func<object> func,
                                       IObjectProvider objectProvider,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget)
        {
            var concurrencyProcessor = objectProvider.GetService<IConcurrencyProcessor>();
            return concurrencyProcessor.Process(func, RetryTimes);
        }
    }
}