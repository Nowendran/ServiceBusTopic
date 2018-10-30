﻿using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;  //Required to access azure services

namespace ConsoleApp1
{
    class Program
    {
        // servicebus resource needs to be created on azure, and then on that service buse you creaate a topic and a subscription
        //the following three lines stores the service bus stuff into variable
        const string ServiceBusConnectionString = "Endpoint=sb://nowendran.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=WLg0pwDbMVKEXigdt5BfOQv+1Nvfj2LRGO/tBgeV5sI=";
        const string TopicName = "nowen";
        const string SubscriptionName = "bro";
        
        // uses the using statement 
        // following variables are instances required to get or send data to the service bus
        static ISubscriptionClient _subscriptionClient;
        static ITopicClient _topicClient;

        static void Main(string[] args)
        {
            //following line sends messages to the service using the MainAsync (Refer to last 2 methods Method name is MainAsync)
            MainAsync().GetAwaiter().GetResult();
            // This recieves the messages using the method directly after this
            RecieveMainAsync().GetAwaiter().GetResult();
        }

        static async Task RecieveMainAsync()
        {
            // this im assuming specifies which service bus/ Topic and subscription yourre connecting too on your azure account
           
            _subscriptionClient = new SubscriptionClient(ServiceBusConnectionString, TopicName, SubscriptionName);


            //i dont wanna explain this
            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
            Console.WriteLine("======================================================");

            // Register subscription message handler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();

            await _subscriptionClient.CloseAsync();
        }
        //message handler
        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                AutoComplete = false
            };

            // Register the function that processes messages.
            _subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }
        //this basically processes the message
        //the way it is displayed in your app and encoding it from azure is done here
    
        static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Process the message.
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            // Complete the message so that it is not received again.
            // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is the default).
            await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
            // If subscriptionClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }

        // Use this handler to examine the exceptions received on the message pump.
        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        #region Send Message To Server
            static async Task MainAsync()
            {
                const int numberOfMessages = 10;
                _topicClient = new TopicClient(ServiceBusConnectionString, TopicName);

                Console.WriteLine("======================================================");
                Console.WriteLine("Press ENTER key to exit after sending all the messages.");
                Console.WriteLine("======================================================");

                // Send messages.
                await SendMessagesAsync(numberOfMessages);

                Console.ReadKey();

                await _topicClient.CloseAsync();
        }
            static async Task SendMessagesAsync(int numberOfMessagesToSend)
            {
                try
                {
                    for (var i = 0; i < numberOfMessagesToSend; i++)
                    {
                        // Create a new message to send to the topic.
                        string messageBody = $"Message {i}";
                        var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                        // Write the body of the message to the console.
                        Console.WriteLine($"Sending message: {messageBody}");

                        // Send the message to the topic.
                        await _topicClient.SendAsync(message);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
                }
        }
        #endregion

    }
}