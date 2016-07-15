using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using DistributedComputingNetwork.CalculationCore;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.NetworkDispatcher;
using DistributedComputingNetwork.NetworkMonitor;
using DistributedComputingNetwork.NetworkMonitorApplication.ApplicationSubsystems;
using DistributedComputingNetwork.PipeConnection;
using DistributedComputingNetwork.SubsystemInterfaces;
using Monitor = DistributedComputingNetwork.NetworkMonitor.Monitor;

namespace DistributedComputingNetwork.NetworkMonitorApplication
{
    public partial class Form1 : Form
    {
        private readonly TaskScheduler _uiTaskScheduler;
        private TcpClient selectedClient;

        private TextMessageSubsystem textMessageSubsystem;
        private CalculationManager calculationSubsystem;
        private ApplicationConnector pipeSubsystem;
        private AssemblySubsystem assemblySubsystem;
        private LoggerSubsystem loggerSubsystem;

        private object newConnection;
        private dynamic lostConnection;

        public Form1()
        {
            InitializeComponent();
            networkMonitor = new Monitor();
            _uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            //initialize subsystems
            textMessageSubsystem = new TextMessageSubsystem(this);
            loggerSubsystem = new LoggerSubsystem(this);
            calculationSubsystem = new CalculationManager();
            pipeSubsystem = new ApplicationConnector();
            assemblySubsystem = new AssemblySubsystem();

            //binding
            Dispatcher.AddNotification(textMessageSubsystem,InformationType.TextMessage);
            Dispatcher.AddNotification(calculationSubsystem,InformationType.DelegateAndImmediateExecute);
            Dispatcher.AddNotification(calculationSubsystem, InformationType.ExpressionAndImmediateExecute);
            Dispatcher.AddNotification(assemblySubsystem, InformationType.Assembly);
            Dispatcher.AddNotification(calculationSubsystem, InformationType.Assembly);
            Dispatcher.AddNotification(pipeSubsystem, InformationType.CalculationResult);
            Dispatcher.Logger = loggerSubsystem;

            //change listbox on new connection
            networkMonitor.NewConnection += (o, args) =>
            {
                TcpClient client = networkMonitor.Sockets[networkMonitor.Addresses.IndexOf(args.Address)];
                ConnectionDispatcher dispatcher = new ConnectionDispatcher(client);
                Task<string> task = new Task<string>(() => args.Address.ToString());
                task.ContinueWith(delegate (Task<string> task1)
                {
                    listBox1.Items.Add(task1.Result);
                },
                    _uiTaskScheduler);
                task.Start();
            };

            //change listbox on connection loss
            networkMonitor.LostConnection += (o, args) =>
            {
               /* ConnectionDispatcher disp =
                    Dispatcher.Dispatchers.First(
                        dispatcher =>
                            dispatcher.Connection ==
                            networkMonitor.Sockets[networkMonitor.Addresses.IndexOf(args.Address)]);*/
                Task<string> task = new Task<string>(() => args.Address.ToString());
                task.ContinueWith(delegate (Task<string> task1) { listBox1.Items.Remove(task1.Result); }, _uiTaskScheduler);
                task.Start();
            };

            //binding progressbars
            networkMonitor.ScanState.PercentState = new Progress<int>(i => progressBar4.Value = i);
            networkMonitor.ScanState.Message = new Progress<string>(i => label3.Text = i);
        }

        public void ShowTextMessage(string message)
        {
            Task task = new Task(() => { });
            task.ContinueWith(delegate { MessageBox.Show(message); }, _uiTaskScheduler);
            task.Start();
        }

        private Monitor networkMonitor;

        private void onButton_Click(object sender, EventArgs e)
        {
            networkMonitor.Start();
            onButton.Enabled = false;
            offButton.Enabled = true;
        }

        private void offButton_Click(object sender, EventArgs e)
        {
            networkMonitor.Stop();
            listBox1.Items.Clear();

            offButton.Enabled = false;
            onButton.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //send message to another cell
            string s = listBox1.SelectedItem.ToString();
            int index = networkMonitor.Addresses.IndexOf(IPAddress.Parse(s));
            textMessageSubsystem.SendMessage(Dispatcher.Dispatchers[index], messageBox.Text);
            messageBox.Text = "";
        }

        public void AddLog(string s)
        {
            Task task = new Task(() => { });
            task.ContinueWith(delegate { logBox.AppendText(s + '\n'); }, _uiTaskScheduler);
            task.Start();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button3.Enabled = listBox1.SelectedItem != null;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            pipeSubsystem.Dispose();
            networkMonitor.Stop();
            foreach(ConnectionDispatcher disp in Dispatcher.Dispatchers.ToArray())
            {
                disp.Dispose();
            }
        }
    }
}
