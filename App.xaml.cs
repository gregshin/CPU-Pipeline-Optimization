using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Hazard_Detection
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // models cpu registers
        public class Cpu
        {
            public Cpu()
            {
                //r1 = 0;
                //r2 = 0;
                //r3 = 0;
                //r4 = 0;
                //r5 = 0;

                Pipeline = new List<Instruction>();
                Stalled = new List<List<char>>();
                Forwarding = new List<List<char>>();
            }

            //public int r1;
            //public int r2;
            //public int r3;
            //public int r4;
            //public int r5;

            // holds pipeline instructions
            public List<Instruction> Pipeline;
            public List<List<char>> Stalled;
            public List<List<char>> Forwarding;
        }

        public class Instruction
        {
            public Instruction(string inst, string r1, string r2)
            {
                instruct = inst;
                destReg = r1;
                srcReg1 = r2;
            }
            public string instruct;
            public string destReg;
            public string srcReg1;
            public char forwAvail;
            public char forwNeed;
            public char normAvail;
            public char normNeeded;
        }

        // models r-type instruction
        public class RType : Instruction
        {
            public RType(string inst, string r1, string r2, string r3) : base(inst, r1, r2)
            {
                srcReg2 = r3;
                normAvail = 'W';
                normNeeded = 'D';
                forwAvail = 'M';
                forwNeed = 'X';
            }
            public string srcReg2;
        }
        // models i-type instructions
        public class IType : Instruction
        {
            public IType(string inst, string r1, string r2, int i) : base(inst, r1, r2)
            {
                imm = i;
            }
            public int imm;
        }
        // special class for memory access instructions
        public class Mem : Instruction
        {
            public Mem(string inst, string r1, string r2, string off) : base(inst, r1, r2)
            {
                offset = off;

                forwAvail = 'M'; // avail after execution
                forwNeed = 'X'; // needed before execution
                normAvail = 'W';
                normNeeded = 'D';
            }
            string offset;
        }

        public class Load : Mem
        {
            public Load(string inst, string r1, string r2, string off) : base(inst, r1, r2, off)
            {
                forwAvail = 'W'; // avail after mem access
                forwNeed = 'M';
                normAvail = 'W';
                normNeeded = 'D';
            }
        }

        public class Store : Mem
        {
            public Store(string inst, string r1, string r2, string off) : base(inst, r1, r2, off)
            {
                forwAvail = 'W'; // avail after mem access
                forwNeed = 'X'; // ALU calculates the effective address
            }
        }
    }
}
