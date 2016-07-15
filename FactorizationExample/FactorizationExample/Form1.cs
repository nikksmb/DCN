using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DistributedComputingNetwork.DCN;

namespace FactorizationExample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            results = new Dictionary<ulong, ulong[]>();
            factor = Factorization.Facrotize_v1;
            Dcn.Initialize();
        }

        private Dictionary<ulong, ulong[]> results;

        private Func<ulong,ulong[]> factor;

        //private Factor factor; 

        private ulong[] numbers = new ulong[]
        {
            569078771992631183,
            169735540918442683,
            294729986272467869,
            307862235175117591,
            4323760110887431,
            5109733579954,
            318802315442280409,
            715090939,
            92003943153861689,
            699074142919499729,
            77677061650839097,
            607992783237840023,
            607964371567931401,
            257622230728379827,
            193047859265156999,
            49810159047795689,
            121464151476123999,
            196981155860193137,
            574370250620857087,
            358379592147428801,
            494493089965976389,
            168097187443253009,
            303414091809240553,
            274777940539816031,
            41263962700454597,
            181056723710015147,
            532739315875767733,
            725099743844499187,
            107836297262276537,
            259369255354638817,
            155834424230656903,
            638881045912002539
        };

        private void button1_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            results.Clear();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            if (radioButton1.Checked)
            {
                int count = listBox1.Items.Count;
                ulong[] start = new ulong[count];
                Semaphore s = new Semaphore(0, count);
                for (int i = 0; i<count; i++)
                {
                    ulong val = ulong.Parse(listBox1.Items[i].ToString());
                    start[i] = val;
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        ulong[] res = factor(val);
                        lock (results)
                        {
                            results.Add(val, res);
                        }
                        s.Release();
                    });
                }
                for (int i = 0; i < count; i++)
                {
                    s.WaitOne();
                }
                lock (results)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string item = "";
                        for (int j = 0; j < results[start[i]].Count(); j++)
                        {
                            item += results[start[i]][j].ToString() + " ";
                        }
                        listBox2.Items.Add(item);
                    }
                }
            }
            if (radioButton2.Checked)
            {
                int count = listBox1.Items.Count;
                Semaphore s = new Semaphore(0, count);
                ulong[] start = new ulong[count];
                for (int i = 0; i<count; i++)
                {
                    ulong val = ulong.Parse(listBox1.Items[i].ToString());
                    start[i] = val;
                    if (i < count*2/3)
                    {
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            
                            ulong[] res = factor(val);
                            lock(results)
                            {
                                results.Add(val , res);
                            }
                            s.Release();
                        });
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(o => 
                        {
                            ulong[] res = Dcn.ParametrizedDelegate(factor, val);
                            lock (results)
                            {
                                results.Add(val, res);
                            }
                            s.Release();
                        });
                    }
                }
                for (int i = 0; i<count; i++)
                {
                    s.WaitOne();
                }
                lock (results)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string item = "";
                        for (int j = 0; j< results[start[i]].Count(); j++)
                        {
                            item += results[start[i]][j].ToString() + " ";
                        }
                        listBox2.Items.Add(item);
                    }
                }
            }
            if (radioButton3.Checked)
            {
                int count = listBox1.Items.Count;
                Semaphore s = new Semaphore(0, count);
                ulong[] start = new ulong[count];
                for (int i = 0; i < count; i++)
                {
                    ulong val = ulong.Parse(listBox1.Items[i].ToString());
                    start[i] = val;
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        ulong[] res = Dcn.ParametrizedDelegate(factor, val);
                        lock (results)
                        {
                            results.Add(val, res);
                        }
                        s.Release();
                    });
                }
                for (int i = 0; i < count; i++)
                {
                    s.WaitOne();
                }
                lock (results)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string item = "";
                        for (int j = 0; j < results[start[i]].Count(); j++)
                        {
                            item += results[start[i]][j].ToString() + " ";
                        }
                        listBox2.Items.Add(item);
                    }
                }
            }
            if (radioButton4.Checked)
            {
                int count = listBox1.Items.Count;
                Semaphore s = new Semaphore(0, count);
                ulong[] start = new ulong[count];
                for (int i = 0; i < count; i++)
                {
                    ulong val = ulong.Parse(listBox1.Items[i].ToString());
                    start[i] = val;
                    if (i < count / 3)
                    {
                        ThreadPool.QueueUserWorkItem(state =>
                        {

                            ulong[] res = factor(val);
                            lock (results)
                            {
                                results.Add(val, res);
                            }
                            s.Release();
                        });
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            ulong[] res = Dcn.ParametrizedDelegate(factor, val);
                            lock (results)
                            {
                                results.Add(val, res);
                            }
                            s.Release();
                        });
                    }
                }
                for (int i = 0; i < count; i++)
                {
                    s.WaitOne();
                }
                lock (results)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string item = "";
                        for (int j = 0; j < results[start[i]].Count(); j++)
                        {
                            item += results[start[i]][j].ToString() + " ";
                        }
                        listBox2.Items.Add(item);
                    }
                }
            }
            watch.Stop();
            label2.Text = (((double) watch.ElapsedMilliseconds)/1000).ToString();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            for(int i = 0; i<24; i++)
            {
                listBox1.Items.Add(numbers[i].ToString());
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            for (int i = 0; i < 16; i++)
            {
                listBox1.Items.Add(numbers[i].ToString());
            }
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            for (int i = 0; i < 32; i++)
            {
                listBox1.Items.Add(numbers[i].ToString());
            }
        }
        
        private void radioButton11_CheckedChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            for (int i = 0; i < 8; i++)
            {
                listBox1.Items.Add(numbers[i].ToString());
            }
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            factor = Factorization.Facrotize_v3;
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            factor = Factorization.Facrotize_v2;
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            factor = Factorization.Facrotize_v1;
        }

        private void radioButton12_CheckedChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            for (int i = 0; i < 48; i++)
            {
                listBox1.Items.Add(numbers[i%32].ToString());
            }
        }

        private void radioButton13_CheckedChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            for (int i = 0; i < 64; i++)
            {
                listBox1.Items.Add(numbers[i % 32].ToString());
            }
        }
    }
}
