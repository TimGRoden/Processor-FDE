using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ProcessorFDE
{
    internal class Program
    {
        static Dictionary<string, string> registers = new Dictionary<string, string>();
        static Dictionary<string, string> memory = new Dictionary<string, string>();
        const int timeDelay = 1000;
        static bool isClick = false;
        static void updateRegisters()
        {
            Console.SetCursorPosition(0, 0);
            foreach (string key in registers.Keys)
            {
                Console.WriteLine($"{key}:{new string(' ',10-key.Length)}{registers[key]}{new string(' ',25-registers[key].Length)}");
            }
        }
        static void updateMemory()
        {
            Console.SetCursorPosition(Console.WindowWidth - 21, 0);
            foreach (string key in memory.Keys)
            {
                Console.Write($"{key}: {memory[key]}");
                Console.CursorTop++;
                Console.CursorLeft = Console.WindowWidth - 21;
            }
        }
        static void Delay()
        {
            if (isClick) Console.ReadKey();
            else System.Threading.Thread.Sleep(timeDelay);
        }
        static string Increment(string binString, int n)
        {
            int val = Convert.ToInt32(binString.Substring(0, 4) + binString.Substring(5), 2);
            val += n;
            string bin = Convert.ToString(val, 2);
            while (bin.Length < 8) bin = "0" + bin; //Add leading 0's.
            return bin.Substring(0,4) + " " + bin.Substring(4);
        }
        static string Read(string location, int bytes)
        {
            string mem = "";
            for (int i = 0; i < bytes; i++)
            {
                mem += memory[location];
                if (i != bytes - 1) mem += " ";     //Add spaces if it's not the end of memory.
                location = Increment(location, 1);  //Look at next memory.
            }
            return mem;
        }
        static void Fetch()
        {   //This fetch needs to load two registers of data. Instructions are 16 bit.
            registers["MAR"] = registers["PC"];             //The contents of the PC are copied into the MAR
            updateRegisters(); Delay();

            registers["MBR"] = Read(registers["MAR"], 2);   //The address bus is used to transfer this address to main memory
            updateRegisters(); Delay();                     //The instruction held at that address in main memory is transferred via the data bus to the MBR

            registers["PC"] = Increment(registers["PC"],2); //The PC is incremented to hold the address of the next instruction to be executed
            updateRegisters(); Delay();

            registers["CIR"] = registers["MBR"];            //The contents of the MBR are copied into the CIR
            updateRegisters(); Delay();
        }
        static void Decode()
        { // Take the string, break it down, re-read.
            if (registers["CIR"]=="0000 0000 0000 0000")        //If it's an end command, stop.
            {
                registers["Decode"] = "END";
                return;
            }
            string instr = "";
            string[] nibbles = registers["CIR"].Split(' ');
            switch (nibbles[0])
            { //Type of instruction
                case "0001": //Load
                    instr += "LOAD "; break;
                case "0010": //Store
                    instr += "STORE "; break;
                case "0011": //Add
                    instr += "ADD "; break;
                case "0100": //AddI
                    instr += "ADDI "; break;
                case "0101": //Sub
                    instr += "SUB "; break;
            }
            instr += "R" + Convert.ToInt32(nibbles[1], 2) + ", "; //Target register.
            
            switch (nibbles[0])
            { //Different instructions use the remaining digits differently.
                case "0001": 
                case "0010": instr += nibbles[2] + " " + nibbles[3]; break; //Both load and store need a memory location
                case "0011": //Add & Sub both take two registers.
                case "0101": instr += "R" + Convert.ToInt32(nibbles[2], 2) + ", " + "R" + Convert.ToInt32(nibbles[3], 2); break;
                case "0100": instr += "#" + Convert.ToInt32(nibbles[2] + nibbles[3], 2); break; //Literal values indicated with #
            }
            registers["Decode"] = instr;
            updateRegisters(); Delay();
        }
        static void Execute() 
        {
            string[] instr = registers["Decode"].Split(' ');
            switch (instr[0])
            {
                case "LOAD":
                    {
                        break;
                    }
                case "STORE":
                    {
                        break;
                    }
                case "ADD":
                    {
                        break;
                    }
                case "ADDI":
                    {
                        break;
                    }
                case "SUB":
                    {
                        break;
                    }
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Click or Time-Delay? ('click' or otherwise)");
            isClick = Console.ReadLine().ToLower() == "click";
            Console.Clear();
            registers.Add("PC", "0000 0000");
            registers.Add("CIR", "0000 0000");
            registers.Add("MAR", "0000 0000");
            registers.Add("MBR", "0000 0000");
            registers.Add("R0", "0000 0000");
            registers.Add("R1", "0000 0000");
            registers.Add("Decode", " ");
            updateRegisters();
            using (StreamReader sr = new StreamReader(new FileStream("mainMemory.txt", FileMode.OpenOrCreate)))
            {
                while (!sr.EndOfStream)
                {
                    string[] mem = sr.ReadLine().Split('.');
                    memory.Add(mem[0], mem[1]);
                }
            }
            updateMemory();
            while (registers["Decode"] != "END")
            { //Loop until end command occurs.
                Fetch();
                Decode();
                Execute();
            }
            Console.ReadKey();
        }
    }
}
