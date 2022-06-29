
using Microsoft.Azure.ServiceBus;
using System.Text;

public class ServiceBusRun
{
    const string QueueConnectionString = "";
    const string QueuePath = "ProductChanged";
    static IQueueClient _queueClient;

    private static void Main(List<string> args)
    {
        if (args[0].Length <= 0 || args[0] == "sender")
        {
            SendMessagesAsync().GetAwaiter().GetResult();
            Console.WriteLine("messages were sent");
        }
        else if (args[0] == "receiver")
        {
            ReceiveMessagesAsync().GetAwaiter().GetResult();
            Console.WriteLine("messages were received");
        }
        else
            Console.WriteLine("nothing to do");

        Console.ReadLine();
    }

    private static async Task SendMessagesAsync()
    {
        var queueClient = new QueueClient(QueueConnectionString, QueuePath);

        queueClient.OperationTimeout = TimeSpan.FromSeconds(10);

        var messages = " Hi,Hello,Hey,How are you,Be Welcome"
        .Split(',')
        .Select(msg =>
        {
            Console.WriteLine($"Will send message: {msg}");
            return new Message(Encoding.UTF8.GetBytes(msg));
        })
        .ToList();

        var sendTask = queueClient.SendAsync(messages);

        await sendTask;

        CheckCommunicationExceptions(sendTask);

        var closeTask = _queueClient.CloseAsync();

        await closeTask;

        CheckCommunicationExceptions(closeTask);
    }

    private static async Task ReceiveMessagesAsync()
    {
        _queueClient = new QueueClient(QueueConnectionString, QueuePath);
        _queueClient.RegisterMessageHandler(MessageHandler,
            new MessageHandlerOptions(ExceptionHandler) { AutoComplete = false });
        Console.ReadLine();
        await _queueClient.CloseAsync();
    }


    private static Task ExceptionHandler(ExceptionReceivedEventArgs exceptionArgs)
    {
        Console.WriteLine($"Message handler encountered an exception {exceptionArgs.Exception}.");
        var context = exceptionArgs.ExceptionReceivedContext;
        Console.WriteLine($"Endpoint:{context.Endpoint}, Path:{context.EntityPath}, Action:{context.Action}");
        return Task.CompletedTask;
    }

    private static async Task MessageHandler(Message message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received message {Encoding.UTF8.GetString(message.Body)}");

        if (cancellationToken.IsCancellationRequested ||
            _queueClient.IsClosedOrClosing)
            return;

        Console.WriteLine($"task");
        Task PendingTask;

        Console.WriteLine($"calling complete for task ");
        Console.WriteLine($"remove task from task queue");
    }

    public static bool CheckCommunicationExceptions(Task task)
    {
        if (task.Exception == null || task.Exception.InnerExceptions.Count == 0) return true;

        task.Exception.InnerExceptions.ToList()
        .ForEach(innerException =>
        {
            Console.WriteLine(@$"Error in SendAsync task:  
                { innerException.Message}.  
                Details:
                { innerException.StackTrace}");

            if (innerException is ServiceBusCommunicationException)
                Console.WriteLine("Connection Problem with Host");
        });

        return false;
    }
}