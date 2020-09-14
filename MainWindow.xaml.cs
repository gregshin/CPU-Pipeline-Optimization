﻿using System;
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

            printValidInst();
        }

        public App.Cpu cpu;

        private void printValidInst()
        {
            dispInst.Items.Add("add");
            dispInst.Items.Add("sub");
            dispInst.Items.Add("div");
            dispInst.Items.Add("mult");
            dispInst.Items.Add("and");
            dispInst.Items.Add("or");
            dispInst.Items.Add("lw");
            dispInst.Items.Add("sw");
        }

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

                // create instruction objects based on input
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
                    string[] tempStr = parts[2].Split(new string[] { "(" }, StringSplitOptions.None);
                    tempStr[1] = tempStr[1].TrimEnd(')');

                    cpu.Pipeline.Add(new App.Store("sw", tempStr[1], parts[1], tempStr[0]));
                }
            }

            stall(ref cpu);
            forward(ref cpu);
            printStalled(ref cpu);
            printForwarded(ref cpu);
            printRAW(ref cpu);
            printWAW(ref cpu);
        }
        // method to create pipeline with stalls
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
                    // figure out how many to indent if based on previous D (F goes under D)
                    int indent = cpu.Stalled[i - 1].FindIndex(item => item == 'D');

                    // add indents
                    for (int n = 0; n < indent; n++)
                    {
                        cpu.Stalled[i].Add(' ');
                    }
                    // add F stage
                    cpu.Stalled[i].Add('F');

                    // if the current instruction is an R Type
                    if (cpu.Pipeline[i] is App.RType)
                    {
                        // cast as R Type
                        var tempInst = cpu.Pipeline[i] as App.RType;

                        // start count at 1
                        int count = 1;

                        // check 4 instructions back
                        while (i - count >= 0 && count < 4)
                        {
                            // if there is a RAW hazard
                            if (cpu.Pipeline[i - count].destReg == tempInst.srcReg1 || cpu.Pipeline[i - count].destReg == tempInst.srcReg2)
                            {
                                // check instruction for when register is needed
                                char need = cpu.Pipeline[i].normNeed;
                                // check instruction for when register is available
                                char avail = cpu.Pipeline[i - count].normAvail;

                                // use needNum to find difference
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

                                // only have F right now, so calculate how many spaces needed based on F index
                                int j = cpu.Stalled[i].FindIndex(item => item == 'F');
                                j += needNum;

                                int k = cpu.Stalled[i - count].FindIndex(item => item == avail);

                                int diff = k - j;

                                // if arrow is pointing backwards
                                if (diff > 0)
                                {
                                    // add stalls
                                    for (int n = 0; n < diff; n++)
                                    {
                                        cpu.Stalled[i].Add('-');
                                    }

                                    cpu.Hazards.Add("RAW Hazard Detected on lines " + (i - count) + " and " + i);
                                }
                            }
                            count++;
                        }
                        // add rest of stages
                        cpu.Stalled[i].Add('D');
                        cpu.Stalled[i].Add('X');
                        cpu.Stalled[i].Add('M');
                        cpu.Stalled[i].Add('W');
                    }

                    // if the current instruction is load type
                    else if (cpu.Pipeline[i] is App.Load)
                    {
                        // cast as R Type
                        var tempInst = cpu.Pipeline[i] as App.Load;

                        // start count at 1
                        int count = 1;

                        // check 4 instructions back
                        while (i - count >= 0 && count < 4)
                        {
                            // if there is a RAW hazard
                            if (cpu.Pipeline[i - count].destReg == tempInst.srcReg1)
                            {
                                // check instruction for when register is needed
                                char need = cpu.Pipeline[i].normNeed;
                                // check instruction for when register is available
                                char avail = cpu.Pipeline[i - count].normAvail;

                                // use needNum to find difference
                                int needNum;

                                switch (need)
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

                                // only have F right now, so calculate how many spaces needed based on F index
                                int j = cpu.Stalled[i].FindIndex(item => item == 'F');
                                j += needNum;

                                int k = cpu.Stalled[i - count].FindIndex(item => item == avail);

                                int diff = k - j;

                                // if arrow is pointing backwards
                                if (diff > 0)
                                {
                                    // add stalls
                                    for (int n = 0; n < diff; n++)
                                    {
                                        cpu.Stalled[i].Add('-');
                                    }

                                    cpu.Hazards.Add("RAW Hazard Detected on lines " + (i - count) + " and " + i);
                                }
                            }
                            count++;
                        }
                        // add rest of stages
                        cpu.Stalled[i].Add('D');
                        cpu.Stalled[i].Add('X');
                        cpu.Stalled[i].Add('M');
                        cpu.Stalled[i].Add('W');
                    }

                    // if the current instruction is store type
                    else if (cpu.Pipeline[i] is App.Store)
                    {
                        // cast as R Type
                        var tempInst = cpu.Pipeline[i] as App.Store;

                        // start count at 1
                        int count = 1;

                        // check 4 instructions back
                        while (i - count >= 0 && count < 4)
                        {
                            // if there is a RAW hazard
                            if (cpu.Pipeline[i - count].destReg == tempInst.srcReg1 || cpu.Pipeline[i - count].destReg == tempInst.srcReg2)
                            {
                                // check instruction for when register is needed
                                char need = cpu.Pipeline[i].normNeed;
                                // check instruction for when register is available
                                char avail = cpu.Pipeline[i - count].normAvail;

                                // use needNum to find difference
                                int needNum;

                                switch (need)
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

                                // only have F right now, so calculate how many spaces needed based on F index
                                int j = cpu.Stalled[i].FindIndex(item => item == 'F');
                                j += needNum;

                                int k = cpu.Stalled[i - count].FindIndex(item => item == avail);

                                int diff = k - j;

                                // if arrow is pointing backwards
                                if (diff > 0)
                                {
                                    // add stalls
                                    for (int n = 0; n < diff; n++)
                                    {
                                        cpu.Stalled[i].Add('-');
                                    }

                                    cpu.Hazards.Add("RAW Hazard Detected on lines " + (i - count) + " and " + i);
                                }
                            }
                            count++;
                        }
                        // add rest of stages
                        cpu.Stalled[i].Add('D');
                        cpu.Stalled[i].Add('X');
                        cpu.Stalled[i].Add('M');
                        cpu.Stalled[i].Add('W');
                    }
                }
            }
        }
        // method to create pipeline with forwarding
        public void forward(ref App.Cpu cpu)
        {
            for (int i = 0; i < cpu.Pipeline.Count; i++)
            {
                // add new line to pipeline
                cpu.Forwarding.Add(new List<char>());

                // if first in list, don't need stalls
                if (i == 0)
                {
                    cpu.Forwarding[i].Add('F');
                    cpu.Forwarding[i].Add('D');
                    cpu.Forwarding[i].Add('X');
                    cpu.Forwarding[i].Add('M');
                    cpu.Forwarding[i].Add('W');
                }
                // else if (i == 1)
                else
                {
                    // figure out how many to indent if based on previous D
                    int indent = cpu.Forwarding[i - 1].FindIndex(item => item == 'D');

                    // add indents
                    for (int n = 0; n < indent; n++)
                    {
                        cpu.Forwarding[i].Add(' ');
                    }
                    // add F stage
                    cpu.Forwarding[i].Add('F');

                    // if the current instruction is an R Type
                    if (cpu.Pipeline[i] is App.RType)
                    {
                        // cast as R Type
                        var tempInst = cpu.Pipeline[i] as App.RType;

                        // start count at 1
                        int count = 1;

                        // check 4 instructions back
                        while (i - count >= 0 && count < 4)
                        {
                            // if there is a RAW hazard
                            if (cpu.Pipeline[i - count].destReg == tempInst.srcReg1 || cpu.Pipeline[i - count].destReg == tempInst.srcReg2)
                            {
                                // check instruction for when register is needed
                                char need = cpu.Pipeline[i].forwNeed;
                                // check instruction for when register is available
                                char avail = cpu.Pipeline[i - count].forwAvail;

                                // use needNum to find difference
                                int needNum;

                                switch (need)
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

                                // only have F right now, so calculate how many spaces needed based on F index
                                int j = cpu.Forwarding[i].FindIndex(item => item == 'F');
                                j += needNum;

                                int k = cpu.Forwarding[i - count].FindIndex(item => item == avail);

                                int diff = k - j;

                                // if arrow is pointing backwards
                                if (diff > 0)
                                {
                                    // add stalls
                                    for (int n = 0; n < diff; n++)
                                    {
                                        cpu.Forwarding[i].Add('-');
                                    }
                                }
                            }
                            count++;
                        }
                        // add rest of stages
                        cpu.Forwarding[i].Add('D');
                        cpu.Forwarding[i].Add('X');
                        cpu.Forwarding[i].Add('M');
                        cpu.Forwarding[i].Add('W');
                    }

                    // if the current instruction is load type
                    else if (cpu.Pipeline[i] is App.Load)
                    {
                        // cast as R Type
                        var tempInst = cpu.Pipeline[i] as App.Load;

                        // start count at 1
                        int count = 1;

                        // check 4 instructions back
                        while (i - count >= 0 && count < 4)
                        {
                            // if there is a RAW hazard
                            if (cpu.Pipeline[i - count].destReg == tempInst.srcReg1)
                            {
                                // check instruction for when register is needed
                                char need = cpu.Pipeline[i].forwNeed;
                                // check instruction for when register is available
                                char avail = cpu.Pipeline[i - count].forwAvail;

                                // use needNum to find difference
                                int needNum;

                                switch (need)
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

                                // only have F right now, so calculate how many spaces needed based on F index
                                int j = cpu.Forwarding[i].FindIndex(item => item == 'F');
                                j += needNum;

                                int k = cpu.Forwarding[i - count].FindIndex(item => item == avail);

                                int diff = k - j;

                                // if arrow is pointing backwards
                                if (diff > 0)
                                {
                                    // add stalls
                                    for (int n = 0; n < diff; n++)
                                    {
                                        cpu.Forwarding[i].Add('-');
                                    }
                                }
                            }
                            count++;
                        }
                        // add rest of stages
                        cpu.Forwarding[i].Add('D');
                        cpu.Forwarding[i].Add('X');
                        cpu.Forwarding[i].Add('M');
                        cpu.Forwarding[i].Add('W');
                    }

                    // if the current instruction is store type
                    else if (cpu.Pipeline[i] is App.Store)
                    {
                        // cast as R Type
                        var tempInst = cpu.Pipeline[i] as App.Store;

                        // start count at 1
                        int count = 1;

                        // check 4 instructions back
                        while (i - count >= 0 && count < 4)
                        {
                            // if there is a RAW hazard
                            if (cpu.Pipeline[i - count].destReg == tempInst.srcReg1 || cpu.Pipeline[i - count].destReg == tempInst.srcReg2)
                            {
                                // check instruction for when register is needed
                                char need = cpu.Pipeline[i].forwNeed;
                                // check instruction for when register is available
                                char avail = cpu.Pipeline[i - count].forwAvail;

                                // use needNum to find difference
                                int needNum;

                                switch (need)
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

                                // only have F right now, so calculate how many spaces needed based on F index
                                int j = cpu.Forwarding[i].FindIndex(item => item == 'F');
                                j += needNum;

                                int k = cpu.Forwarding[i - count].FindIndex(item => item == avail);

                                int diff = k - j;

                                // if arrow is pointing backwards
                                if (diff > 0)
                                {
                                    // add stalls
                                    for (int n = 0; n < diff; n++)
                                    {
                                        cpu.Forwarding[i].Add('-');
                                    }
                                }
                            }
                            count++;
                        }
                        // add rest of stages
                        cpu.Forwarding[i].Add('D');
                        cpu.Forwarding[i].Add('X');
                        cpu.Forwarding[i].Add('M');
                        cpu.Forwarding[i].Add('W');
                    }
                }
            }
        }
        // method to output stalled instructions to gui
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
        // method to output forwarded instructions to gui
        public void printForwarded(ref App.Cpu cpu)
        {
            foreach (List<char> s in cpu.Forwarding)
            {
                string line = "";

                foreach (char c in s)
                {
                    line += c;
                }

                opt.Items.Add(line);
            }
        }
        // print hazards to main window
        public void printRAW(ref App.Cpu cpu)
        {
            foreach (string s in cpu.Hazards)
            {
               warn.Items.Add(s);
            }
        }
        // method to detect and print WAW hazards
        private void printWAW(ref App.Cpu cpu)
        {
            List<string> waw = new List<string>();
            
            // iterate through the instructino list
            for (int i = 0; i < cpu.Pipeline.Count(); i++)
            {
                string currDest = cpu.Pipeline[i].destReg;
                List<int> codeLine = new List<int>();

                if (!waw.Contains(currDest))
                {
                    string wawList = "Warning: Potential WAW hazard on lines ";

                    waw.Add(currDest);
                    // check all instructions that come later
                    for (int n = i + 1; n < cpu.Pipeline.Count(); n++)
                    {
                        string nextDest = cpu.Pipeline[n].destReg;

                        // if there is a WAW
                        if (currDest == nextDest)
                        {
                            // if the current codeline isn't already in the list
                            if (!codeLine.Contains(i))
                            {
                                // add that line to the list
                                codeLine.Add(i);
                            }
                            // add future line to the list
                            codeLine.Add(n);
                        }
                    }
                    // if the codeline list isn't empty
                    if (codeLine.Count() > 0)
                    {
                        // iterate through and add to wawList
                        foreach (int line in codeLine)
                        {
                            wawList += line + " ";
                        }
                        // add to the hazard warnings list
                        warn.Items.Add(wawList);
                    }
                    
                }
            }
        }

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            // clear gui elements
            mips.Clear();
            notOpt.Items.Clear();
            opt.Items.Clear();

            // clear all lists
            cpu.Pipeline.Clear();
            cpu.Stalled.Clear();
            cpu.Forwarding.Clear();
        }
    }
}
