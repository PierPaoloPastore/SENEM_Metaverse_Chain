using System;
using System.Numerics;
using System.Threading.Tasks;
using ChainSafe.Gaming.Evm.Transactions;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using UnityEngine;
using ChainSafe.Gaming.RPC.Events;



namespace ChainSafe.Gaming.Evm.Contracts.Custom
{
    public partial class LessonRegistry : ICustomContract
    {
        public string Address => OriginalContract.Address;
       
        public string ABI => "[ 	{ 		\"inputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			}, 			{ 				\"internalType\": \"string\", 				\"name\": \"cid\", 				\"type\": \"string\" 			} 		], 		\"name\": \"registerLesson\", 		\"outputs\": [], 		\"stateMutability\": \"nonpayable\", 		\"type\": \"function\" 	}, 	{ 		\"inputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			} 		], 		\"name\": \"getLesson\", 		\"outputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"cid\", 				\"type\": \"string\" 			}, 			{ 				\"internalType\": \"address\", 				\"name\": \"uploader\", 				\"type\": \"address\" 			} 		], 		\"stateMutability\": \"view\", 		\"type\": \"function\" 	} ]";
        
        public string ContractAddress { get; set; }
        
        public IEventManager EventManager { get; set; }

        public Contract OriginalContract { get; set; }
                
        public bool Subscribed { get; set; }

        
        #region Methods

        public async Task RegisterLesson(string name, string cid, TransactionRequest transactionOverwrite=null) 
        {
            var response = await OriginalContract.Send("registerLesson", new object [] {
                name, cid
            }, transactionOverwrite);
            
            
        }
        public async Task<TransactionReceipt> RegisterLessonWithReceipt(string name, string cid, TransactionRequest transactionOverwrite=null) 
        {
            var response = await OriginalContract.SendWithReceipt("registerLesson", new object [] {
                name, cid
            }, transactionOverwrite);
            
            return response.receipt;
        }

        public async Task<(string cid, string uploader)> GetLesson(string name, TransactionRequest transactionOverwrite=null) 
        {
            var response = await OriginalContract.Call("getLesson", new object [] {
                name
            }, transactionOverwrite);
            
            return ((string)response[0], (string)response[1]);
        }



        #endregion
        
        
        #region Event Classes


        #endregion
        
        #region Interface Implemented Methods
        
        public async ValueTask DisposeAsync()
        {
            
            if(!Subscribed)
                return;
                
           
            Subscribed = false;
            try
            {
                if(EventManager == null)
                    return;


            
            
            }catch(Exception e)
            {
                Debug.LogError("Caught an exception whilst unsubscribing from events\n" + e.Message);
            }
        }
        
        public async ValueTask InitAsync()
        {
            if(Subscribed)
                return;
            Subscribed = true;

            try
            {
                if(EventManager == null)
                    return;

    
            }catch(Exception e)
            {
                Debug.LogError("Caught an exception whilst subscribing to events. Subscribing to events will not work in this session\n" + e.Message);
            }
            
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public IContract Attach(string address)
        {
            return OriginalContract.Attach(address);
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public Task<object[]> Call(string method, object[] parameters = null, TransactionRequest overwrite = null)
        {
            return OriginalContract.Call(method, parameters, overwrite);
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public object[] Decode(string method, string output)
        {
            return OriginalContract.Decode(method, output);
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public Task<object[]> Send(string method, object[] parameters = null, TransactionRequest overwrite = null)
        {
            return OriginalContract.Send(method, parameters, overwrite);
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public Task<(object[] response, TransactionReceipt receipt)> SendWithReceipt(string method, object[] parameters = null, TransactionRequest overwrite = null)
        {
            return OriginalContract.SendWithReceipt(method, parameters, overwrite);
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public Task<HexBigInteger> EstimateGas(string method, object[] parameters, TransactionRequest overwrite = null)
        {
            return OriginalContract.EstimateGas(method, parameters, overwrite);
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public string Calldata(string method, object[] parameters = null)
        {
            return OriginalContract.Calldata(method, parameters);
        }
        
        [Obsolete("It's not advisable to use this method. Use the pre-generated methods instead.")]
        public Task<TransactionRequest> PrepareTransactionRequest(string method, object[] parameters, bool isReadCall = false, TransactionRequest overwrite = null)
        {
            return OriginalContract.PrepareTransactionRequest(method, parameters, isReadCall, overwrite);
        }
        #endregion
    }


}
