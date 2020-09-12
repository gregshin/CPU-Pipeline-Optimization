using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Hazard_Detection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            cpu = new App.Cpu();
        }

        public App.Cpu cpu;

        private void submit_Click(object sender, RoutedEventArgs e)
        {

            // split the user input by line breaks
            string[] inst = mips.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            foreach (string s in inst)
            {
                // replace commas with space
                string temp = s.Replace(",", "");
                // split using space
                string[] parts = temp.Split(new string[] { " " }, StringSplitOptions.None);

                if (parts[0] == "add")
                {
                    cpu.Pipeline.Add(new App.RType("add", parts[1], parts[2], parts[3]));
                }
                else if (parts[0] == "sub")
                {
                    cpu.Pipeline.Add(new App.RType("sub", parts[1], parts[2], parts[3]));
                }
                else if (parts[0] == "lw")
                {
                    string[] tempStr = parts[2].Split(new string[] { "(" }, StringSplitOptions.None);
                    tempStr[1] = tempStr[1].TrimEnd(')');

                    cpu.Pipeline.Add(new App.Load("lw", parts[1], tempStr[1], tempStr[0]));
                }
                else if (parts[0] == "sw")
                {
                    string[] tempStr = parts[1].Split(new string[] { "(" }, StringSplitOptions.None);

                    tempStr[1] = tempStr[1].TrimEnd(')');

                    cpu.Pipeline.Add(new App.Store("sw", tempStr[1], parts[2], tempStr[0]));
                }
            }

            stall(ref cpu);

            printStalled(ref cpu);
        }
        //method to create pipeline with stalls
        public void stall(ref App.Cpu cpu)
        {
            for (int i = 0; i < cpu.Pipeline.Count; i++)
            {
                // add new line to pipeline
                cpu.Stalled.Add(new List<char>());

                // if first in list, don't need stalls
                if (i == 0)
                {
                    cpu.Stalled[i].Add('F');
                    cpu.Stalled[i].Add('D');
                    cpu.Stalled[i].Add('X');
                    cpu.Stalled[i].Add('M');
                    cpu.Stalled[i].Add('W');
                }
                // else if (i == 1)
                else
                {
                    // figure out how many to indent if based on previous D
                    int indent = cpu.Stalled[i - 1].FindIndex(item => item == 'D');

                    // add indents
                    for (int n = 0; n < indent; n++)
                    {
                        cpu.Stalled[i].Add(' ');
                    }
                    // add F stage
                    cpu.Stalled[i].Add('F');

                    if (cpu.Pipeline[i] is App.RType)
                    {
                        var tempInst = cpu.Pipeline[i] as App.RType;

                        int count = 1;

                        while (i - count >= 0 && count < 4)
                        {
                            if (cpu.Pipeline[i - count].destReg == tempInst.srcReg1 || cpu.Pipeline[i - count].destReg == tempInst.srcReg2)
                            {
                                char need = cpu.Pipeline[i].normNeeded;
                                char avail = cpu.Pipeline[i - count].normAvail;

                                int needNum;

                                switch(need)
                                {
                                    case 'F':
                                        needNum = 0;
                                        break;
                                    case 'D':
                                        needNum = 1;
                                        break;
                                    case 'X':
                                        needNum = 2;
                                        break;
                                    case 'M':
                                        needNum = 3;
                                        break;
                                    case 'W':
                                        needNum = 4;
                                        break;
                                    default:
                                        needNum = 5;
                                        break;
                                }

                                // only have F right now, so need to figure out what steps we need to align
                                int j = cpu.Stalled[i].FindIndex(item => item == 'F');
                                j += needNum;

                                int k = cpu.Stalled[i - 1].FindIndex(item => item == avail);

                                int diff = k - j;

                                for (int n = 0; n < diff; n++)
                                {
                                    cpu.Stalled[i].Add('?');
                                }
                            }
                            count++;
                        }
                        cpu.Stalled[i].Add('D');
                        cpu.Stalled[i].Add('X');
                        cpu.Stalled[i].Add('M');
                        cpu.Stalled[i].Add('W');

                        
                    }
                }
            }
        }

        public void printStalled(ref App.Cpu cpu)
        {
            foreach (List<char> s in cpu.Stalled)
            {
                string line = "";

                foreach (char c in s)
                {
                    line += c;
                }

                notOpt.Items.Add(line);
            }
        }

    }
}
