﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("add.rule", HelpText = "Add a rule to existing Aggregator instance in Azure.")]
    class AddRuleCommand : CommandBase
    {
        [Option('i', "instance", HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        [Option('f', "file", HelpText = "Aggregator rule code.")]
        public string File { get; set; }

        internal override async Task<int> RunAsync()
        {
            var azure = AzureLogon.Load()?.Logon();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
            }
            var rules = new AggregatorRules(azure);
            //rules.Progress += Instances_Progress;
            await rules.AddAsync(Instance, Name, File);
            return 0;
        }
    }
}
