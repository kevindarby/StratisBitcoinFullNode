﻿using NBitcoin;
using Stratis.SmartContracts;
using Stratis.SmartContracts.Backend;
using Stratis.SmartContracts.ContractValidation;
using Stratis.SmartContracts.State;
using Stratis.SmartContracts.Util;
using Xunit;

namespace Stratis.Bitcoin.Features.SmartContracts.Tests
{
    public sealed class SmartContractTransactionExecutorTests
    {
        private readonly SmartContractDecompiler decompiler;
        private readonly SmartContractGasInjector gasInjector;
        private readonly ContractStateRepositoryRoot stateRepository;

        public SmartContractTransactionExecutorTests()
        {
            this.decompiler = new SmartContractDecompiler();
            this.gasInjector = new SmartContractGasInjector();
            this.stateRepository = new ContractStateRepositoryRoot(new NoDeleteSource<byte[], byte[]>(new MemoryDictionarySource())); ;
        }

        [Fact]
        public void ExecuteCallContract_Fails_ReturnFundsToSender()
        {
            //Get the contract execution code------------------------
            byte[] contractExecutionCode = GetFileDllHelper.GetAssemblyBytesFromFile("SmartContracts/ThrowSystemExceptionContract.cs");
            //-------------------------------------------------------

            var toAddress = new uint160(1);

            //Call smart contract and add to transaction-------------
            SmartContractCarrier call = SmartContractCarrier.CallContract(1, toAddress, "ThrowException", 1, (Gas)500000);
            byte[] serializedCall = call.Serialize();
            var transactionCall = new Transaction();
            //transactionCall.AddInput(new TxIn());
            TxOut callTxOut = transactionCall.AddOutput(0, new Script(serializedCall));
            callTxOut.Value = 100;
            //-------------------------------------------------------

            var senderAddress = new uint160(2);

            //Deserialize the contract from the transaction----------
            //and get the module definition
            var deserializedCall = SmartContractCarrier.Deserialize(transactionCall, callTxOut);
            deserializedCall.Sender = senderAddress;

            SmartContractDecompilation decompilation = this.decompiler.GetModuleDefinition(contractExecutionCode);
            //-------------------------------------------------------

            this.stateRepository.SetCode(new uint160(1), contractExecutionCode);

            var executor = new SmartContractTransactionExecutor(this.stateRepository, this.decompiler, new SmartContractValidator(new ISmartContractValidator[] { }), this.gasInjector, deserializedCall, 0, 0, deserializedCall.To);
            SmartContractExecutionResult result = executor.Execute();

            Assert.True(result.Revert);
            Assert.Single(result.InternalTransactions);
            Assert.Single(result.InternalTransactions[0].Inputs);
            Assert.Single(result.InternalTransactions[0].Outputs);

            var actualSender = new uint160(result.InternalTransactions[0].Outputs[0].ScriptPubKey.GetDestination().ToBytes());
            Assert.Equal(senderAddress, actualSender);
            Assert.Equal(100, result.InternalTransactions[0].Outputs[0].Value);
        }
    }
}