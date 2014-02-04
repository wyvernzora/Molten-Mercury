using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace MoltenMercury
{
    public partial class RunMCU : Form
    {
        private class InternalConsoleWriter : TextWriter
        {
            public class OnWriteEventArgs : EventArgs
            {
                public OnWriteEventArgs(String content)
                { Content = content; }
                public String Content { get; set; }
            }

            private EventHandler<OnWriteEventArgs> m_onWrite;
            public event EventHandler<OnWriteEventArgs> OnWrite
            { add { m_onWrite += value; } remove { m_onWrite -= value; } }
            private void RaiseOnWrite(String content)
            {
                if (m_onWrite != null)
                    m_onWrite(this, new OnWriteEventArgs(content));
            }

            List<String> m_previousMessages = new List<string>();
            StringBuilder m_currentMessage = new StringBuilder();

            public override void WriteLine(string value)
            {
                m_currentMessage.Append(value + NewLine);
                String currentMessage = m_currentMessage.ToString();
                m_currentMessage.Clear();
                m_previousMessages.Add(currentMessage);

                RaiseOnWrite(currentMessage);
            }
            public override void Write(string value)
            {
                m_currentMessage.Append(value);
                RaiseOnWrite(value);
            }
            public override void Write(char[] buffer)
            {
                m_currentMessage.Append(buffer);
                RaiseOnWrite(new String(buffer));
            }
            public override void Write(char value)
            {
                m_currentMessage.Append(value);
                RaiseOnWrite(value.ToString());
            }
            public override void Write(char[] buffer, int index, int count)
            {
                m_currentMessage.Append(buffer, index, count);
                RaiseOnWrite(new String(buffer, index, count));
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public String Messages
            {
                get
                {
                    StringBuilder sbldr = new StringBuilder();
                    foreach (String s in m_previousMessages)
                        sbldr.Append(s);
                    sbldr.Append(m_currentMessage);
                    return sbldr.ToString();
                }
            }
        }

        MethodInfo mcuEntry = null;

        String[] commands;
        Boolean autoClose;
        Int32 statusCode = 0;
        String message = null;

        public RunMCU(String[] commands, Boolean autoclose)
        {
            InitializeComponent();
            this.Shown += new EventHandler(RunMCU_Shown);

            lblRunningStatus.Text = Localization.LocalizationDictionary.Instance["MCU_RUNNING_STATUS"];
            this.Text = Localization.LocalizationDictionary.Instance["MCU_TITLE"];

            if (commands == null)
                throw new ArgumentNullException();

            this.commands = commands;
            this.autoClose = autoclose;

            Assembly mcu = Assembly.LoadFile(Path.Combine(Application.StartupPath, "mcu.exe"));
            mcuEntry = mcu.CreateInstance("MCU.Program").GetType().GetMethod("MC_Main");
        }

        void RunMCU_Shown(object sender, EventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (Object s, DoWorkEventArgs a) =>
                {
                    InternalConsoleWriter stdout = new InternalConsoleWriter();
                    InternalConsoleWriter stderr = new InternalConsoleWriter();

                    stdout.OnWrite += (Object sd, InternalConsoleWriter.OnWriteEventArgs args) =>
                        {
                            richTextBox1.BeginInvoke(new Action<InternalConsoleWriter.OnWriteEventArgs>((InternalConsoleWriter.OnWriteEventArgs arg) =>
                                {
                                    richTextBox1.AppendText(arg.Content);
                                    richTextBox1.ScrollToCaret();
                                }), args);
                        };
                    stderr.OnWrite += (Object sd, InternalConsoleWriter.OnWriteEventArgs args) =>
                    {
                        richTextBox1.BeginInvoke(new Action<InternalConsoleWriter.OnWriteEventArgs>((InternalConsoleWriter.OnWriteEventArgs arg) =>
                        {
                            richTextBox1.AppendText(arg.Content);
                            richTextBox1.Select(richTextBox1.Text.Length - arg.Content.Length, arg.Content.Length);
                            richTextBox1.SelectionColor = Color.PaleVioletRed;
                            richTextBox1.Select(richTextBox1.Text.Length, 0);
                            richTextBox1.ScrollToCaret();
                        }), args);
                    };

                    foreach (String command in commands)
                    {
                        stdout.WriteLine("Command: {0}\n", command);

                        Int32 status = (Int32)mcuEntry.Invoke(null,
                            new Object[] { stdout, stderr, command });

                        if (status != 0)
                        {
                            statusCode = status;
                            message = stderr.Messages;
                            break;
                        }

                        stdout.WriteLine("\n\n");
                    }
                };
            bw.RunWorkerCompleted += (Object o, RunWorkerCompletedEventArgs a) =>
                {
                    progressBar1.Value = progressBar1.Maximum;
                    progressBar1.Style = ProgressBarStyle.Blocks;

                    if (statusCode != 0)
                    {
                        lblRunningStatus.Text = Localization.LocalizationDictionary.Instance["MCU_FAIL_STATUS"];

                        MessageBox.Show(
                            String.Format(Localization.LocalizationDictionary.Instance["MCU_ERR"],
                                 statusCode,
                                 Localization.LocalizationDictionary.Instance["ERR_" + statusCode.ToString()],
                                 message),
                            Localization.LocalizationDictionary.Instance["MCU_ACTION_FAILED"], MessageBoxButtons.OK, MessageBoxIcon.Error);

                        DebugHelper.ForceDebugPrint("MCU FarCall Reported Error {0}", statusCode);
                        foreach (String mes in message.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            DebugHelper.ForceDebugPrint("\t\t{0}", mes);
                        DebugHelper.ForceDebugPrint("MCU FarCall Console:\n{0}", richTextBox1.Text);

                    }
                    else
                    {
                        lblRunningStatus.Text = Localization.LocalizationDictionary.Instance["MCU_SUCCESS_STATUS"];
                    }


                    String finalMessage = Localization.LocalizationDictionary.Instance["MCU_CLOSE_WINDOW"];
                    richTextBox1.AppendText(finalMessage);

                    if (autoClose)
                        this.Close();
                };
            bw.RunWorkerAsync();
        }

    }
}
