﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EQueue.Clients.Producers;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.EQueue.MessageFormat;
using Microsoft.Extensions.Logging;
using EQueueProtocols = EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueProducer: IMessageProducer
    {
        private readonly ILogger _logger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(typeof(EQueueProducer).Name);
       
        public EQueueProducer(string clusterName, List<IPEndPoint> nameServerList)
        {
            ClusterName = clusterName;
            NameServerList = nameServerList;
        }

        public Producer Producer { get; protected set; }
        public string ClusterName { get; protected set; }
        public List<IPEndPoint> NameServerList { get; protected set; }

        public EQueueProducer Start()
        {
            var setting = new ProducerSetting
            {
                ClusterName = ClusterName,
                NameServerList = NameServerList
            };
            Producer = new Producer(setting).Start();
            return this;
        }

        public void Stop()
        {
            Producer?.Shutdown();
        }

        protected EQueueProtocols.Message GetEQueueMessage(IMessageContext messageContext, string topic)
        {
            topic = Configuration.Instance.FormatMessageQueueName(topic);
            var jsonValue = ((MessageContext) messageContext).EqueueMessage.ToJson();
            return new EQueueProtocols.Message(topic, 1, Encoding.UTF8.GetBytes(jsonValue));
        }

        public async Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var equeueMessage = GetEQueueMessage(messageContext, messageContext.Topic);
            var key = messageContext.Key ?? string.Empty;

            var retryTimes = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                retryTimes++;
                // 每次发送失败后线性增长等待发送时间 如: 5s, 10s, 15s, 20s .... max:5 minutes
                var waitTime = Math.Min(retryTimes * 1000 * 5, 60000 * 5);
                try
                {
                    var result = await Producer.SendAsync(equeueMessage, key)
                                               .ConfigureAwait(false);
                    if (result.SendStatus != SendStatus.Success)
                    {
                        _logger.LogError($"send message failed topic: {equeueMessage.Topic} key:{key} error:{result.ErrorMessage}");
                        await Task.Delay(waitTime, cancellationToken);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"send message failed topic: {equeueMessage.Topic} key:{key}", e);
                    await Task.Delay(waitTime, cancellationToken);
                }
            }
        }
    }
}