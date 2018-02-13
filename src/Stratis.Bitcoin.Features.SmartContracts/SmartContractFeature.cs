﻿using System;
using System.Collections.Generic;
using System.Text;
using DBreeze;
using Microsoft.Extensions.DependencyInjection;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Builder.Feature;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Miner;
using Stratis.SmartContracts.ContractValidation;
using Stratis.SmartContracts.State;

namespace Stratis.Bitcoin.Features.SmartContracts
{
    public class SmartContractFeature : FullNodeFeature
    {
        public override void Initialize()
        {
            throw new NotImplementedException("At this point the feature is only used to inject the new consensus validator");
        }
    }

    public static partial class IFullNodeBuilderExtensions
    {
        public static IFullNodeBuilder AddSmartContracts(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<SmartContractFeature>()
                    .DependOn<ConsensusFeature>()
                    .DependOn<MiningFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<SmartContractDecompiler>();
                        SmartContractValidator validator = new SmartContractValidator(new List<ISmartContractValidator>
                        {
                            new SmartContractFormatValidator(),
                            new SmartContractDeterminismValidator()
                        });
                        services.AddSingleton<SmartContractValidator>(validator);
                        services.AddSingleton<SmartContractGasInjector>();

                        // TODO: Get root from somewhere and get these strings from somewhere
                        DBreezeEngine engine = new DBreezeEngine("C:/data");
                        DBreezeByteStore byteStore = new DBreezeByteStore(engine, "ContractState");
                        ISource<byte[], byte[]> stateDB = new NoDeleteSource<byte[], byte[]>(byteStore);
                        byte[] root = null; 
                        ContractStateRepositoryRoot repository = new ContractStateRepositoryRoot(stateDB, root);
                        services.AddSingleton<IContractStateRepository>(repository);
                        services.AddSingleton<PowConsensusValidator, SmartContractConsensusValidator>();
                        services.AddSingleton<PowBlockAssembler, SmartContractBlockAssembler>();
                    });
            });
            return fullNodeBuilder;
        }
    }
}