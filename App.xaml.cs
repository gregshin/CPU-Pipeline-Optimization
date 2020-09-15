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
                // init lists
                Pipeline = new List<Instruction>();
                Stalled = new List<List<char>>();
                Forwarding = new List<List<char>>();
                Hazards = new List<string>();
                separateMem = false;
            }

            // holds pipeline instructions
            public List<Instruction> Pipeline;
            // holds stalled instructions
            public List<List<char>> Stalled;
            // holds forwarded instructions
            public List<List<char>> Forwarding;
            // holds identified hazards
            public List<string> Hazards;

            public bool separateMem;
        }
        // base class instruction
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
            public char normNeed;
        }

        // models r-type instruction
        public class RType : Instruction
        {
            public RType(string inst, string r1, string r2, string r3) : base(inst, r1, r2)
            {
                srcReg2 = r3;
                normAvail = 'W';
                normNeed = 'D';
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
                normNeed = 'D';
            }
            string offset;
        }
        // special class for load instructions
        public class Load : Mem
        {
            public Load(string inst, string r1, string r2, string off) : base(inst, r1, r2, off)
            {
                forwAvail = 'W'; // avail after mem access
                forwNeed = 'X';
                normAvail = 'W';
                normNeed = 'D';
            }
        }
        // special class for store instructions
        public class Store : Mem
        {
            public Store(string inst, string r1, string r2, string off) : base(inst, null, r1, off)
            {
                forwAvail = 'M'; // avail after mem access
                forwNeed = 'X'; // ALU calculates the effective address
                normAvail = 'M';
                normNeed = 'D';

                srcReg2 = r2;
            }
            public string srcReg2;
        }
    }
}
