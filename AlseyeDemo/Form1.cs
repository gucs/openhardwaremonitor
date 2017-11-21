using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;

namespace AlseyeDemo
{
    public partial class Form1 : Form
    {
        private readonly Computer _computer = new Computer
        {
            CPUEnabled = true,
            GPUEnabled = true,
            HDDEnabled = true,
            MainboardEnabled = true,
            RAMEnabled = true,
            FanControllerEnabled = true
        };

        private readonly List<IHardware> _hardwares = new List<IHardware>();
        private readonly List<ISensor> _sensors = new List<ISensor>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _computer.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Show();

            toolStripStatusLabel1.Text = @"Loading ...";
            toolStripStatusLabel1.Invalidate();
            var sw = new Stopwatch();
            sw.Start();
            _computer.Open();
            sw.Stop();
            toolStripStatusLabel1.Text = $@"Loaded in {sw.Elapsed.TotalSeconds} seconds.";

            InitVar();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateHardware();

            textBox1.Text = GetText();
        }

        private void InitVar()
        {
            LoopHardware(_computer.Hardware);
            _sensors.AddRange(from hw in _hardwares
                from sensor in hw.Sensors
                select sensor);
        }

        private void UpdateHardware()
        {
            foreach (var hw in _hardwares)
                hw.Update();
        }

        private string GetText()
        {
            // group by Hardware Name and Sensor Type
            var q = from sensor in _sensors
                where sensor.SensorType == SensorType.Temperature || sensor.SensorType == SensorType.Load
                group sensor by sensor.Hardware.Name
                into sg
                select new
                {
                    Name = sg.Key,
                    Types = from ss in sg.ToList()
                    group ss by ss.SensorType
                    into ssg
                    select new {Type = ssg.Key, Sensors = ssg.ToList()}
                };

            // build string
            var sb = new StringBuilder();
            foreach (var sensorGroup in q)
            {
                sb.AppendLine(sensorGroup.Name);

                foreach (var type in sensorGroup.Types)
                {
                    sb.Append(new string(' ', 2));
                    sb.AppendLine(type.Type.ToString());

                    foreach (var sensor in type.Sensors)
                    {
                        sb.AppendFormat("{0}{1}, Value = {2}, Min = {3}, Max = {4}",
                            new string(' ', 4), sensor.Name, sensor.Value, sensor.Min, sensor.Max);
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        private void LoopHardware(IHardware[] hardwares)
        {
            foreach (var hw in hardwares)
            {
                if (hw.SubHardware.Length == 0)
                {
                    _hardwares.Add(hw);
                    continue;
                }
                LoopHardware(hw.SubHardware);
            }
        }
    }
}