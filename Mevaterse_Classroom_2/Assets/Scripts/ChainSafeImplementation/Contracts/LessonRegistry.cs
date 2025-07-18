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
       
        public string ABI => "[ 	{ 		\"anonymous\": false, 		\"inputs\": [ 			{ 				\"indexed\": false, 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			}, 			{ 				\"indexed\": false, 				\"internalType\": \"string\", 				\"name\": \"cid\", 				\"type\": \"string\" 			}, 			{ 				\"indexed\": false, 				\"internalType\": \"address\", 				\"name\": \"uploader\", 				\"type\": \"address\" 			} 		], 		\"name\": \"LessonRegistered\", 		\"type\": \"event\" 	}, 	{ 		\"anonymous\": false, 		\"inputs\": [ 			{ 				\"indexed\": false, 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			}, 			{ 				\"indexed\": false, 				\"internalType\": \"string\", 				\"name\": \"newCID\", 				\"type\": \"string\" 			}, 			{ 				\"indexed\": false, 				\"internalType\": \"address\", 				\"name\": \"editor\", 				\"type\": \"address\" 			} 		], 		\"name\": \"LessonUpdated\", 		\"type\": \"event\" 	}, 	{ 		\"inputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			}, 			{ 				\"internalType\": \"string\", 				\"name\": \"cid\", 				\"type\": \"string\" 			} 		], 		\"name\": \"registerLesson\", 		\"outputs\": [], 		\"stateMutability\": \"nonpayable\", 		\"type\": \"function\" 	}, 	{ 		\"inputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			}, 			{ 				\"internalType\": \"string\", 				\"name\": \"newCID\", 				\"type\": \"string\" 			} 		], 		\"name\": \"updateLesson\", 		\"outputs\": [], 		\"stateMutability\": \"nonpayable\", 		\"type\": \"function\" 	}, 	{ 		\"inputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			} 		], 		\"name\": \"exists\", 		\"outputs\": [ 			{ 				\"internalType\": \"bool\", 				\"name\": \"\", 				\"type\": \"bool\" 			} 		], 		\"stateMutability\": \"view\", 		\"type\": \"function\" 	}, 	{ 		\"inputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"name\", 				\"type\": \"string\" 			} 		], 		\"name\": \"getLesson\", 		\"outputs\": [ 			{ 				\"internalType\": \"string\", 				\"name\": \"cid\", 				\"type\": \"string\" 			}, 			{ 				\"internalType\": \"address\", 				\"name\": \"uploader\", 				\"type\": \"address\" 			}, 			{ 				\"internalType\": \"address\", 				\"name\": \"lastEditor\", 				\"type\": \"address\" 			} 		], 		\"stateMutability\": \"view\", 		\"type\": \"function\" 	} ]";
        
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

        public async Task UpdateLesson(string name, string newCID, TransactionRequest transactionOverwrite=null) 
        {
            var response = await OriginalContract.Send("updateLesson", new object [] {
                name, newCID
            }, transactionOverwrite);
            
            
        }
        public async Task<TransactionReceipt> UpdateLessonWithReceipt(string name, string newCID, TransactionRequest transactionOverwrite=null) 
        {
            var response = await OriginalContract.SendWithReceipt("updateLesson", new object [] {
                name, newCID
            }, transactionOverwrite);
            
            return response.receipt;
        }

        public async Task<bool> Exists(string name, TransactionRequest transactionOverwrite=null) 
        {
            var response = await OriginalContract.Call<bool>("exists", new object [] {
                name
            }, transactionOverwrite);
            
            return response;
        }


        public async Task<(string cid, string uploader, string lastEditor)> GetLesson(string name, TransactionRequest transactionOverwrite=null) 
        {
            var response = await OriginalContract.Call("getLesson", new object [] {
                name
            }, transactionOverwrite);
            
            return ((string)response[0], (string)response[1], (string)response[2]);
        }



        #endregion
        
        
        #region Event Classes

        public partial class LessonRegisteredEventDTO : LessonRegisteredEventDTOBase { }
        
        [Event("LessonRegistered")]
        public class LessonRegisteredEventDTOBase : IEventDTO
        {
                    [Parameter("string", "name", 0, false)]
        public virtual string Name { get; set; }
        [Parameter("string", "cid", 1, false)]
        public virtual string Cid { get; set; }
        [Parameter("address", "uploader", 2, false)]
        public virtual string Uploader { get; set; }

        }
    
        public event Action<LessonRegisteredEventDTO> OnLessonRegistered;
        
        private void LessonRegistered(LessonRegisteredEventDTO lessonRegistered)
        {
            OnLessonRegistered?.Invoke(lessonRegistered);
        }

        public partial class LessonUpdatedEventDTO : LessonUpdatedEventDTOBase { }
        
        [Event("LessonUpdated")]
        public class LessonUpdatedEventDTOBase : IEventDTO
        {
                    [Parameter("string", "name", 0, false)]
        public virtual string Name { get; set; }
        [Parameter("string", "newCID", 1, false)]
        public virtual string NewCID { get; set; }
        [Parameter("address", "editor", 2, false)]
        public virtual string Editor { get; set; }

        }
    
        public event Action<LessonUpdatedEventDTO> OnLessonUpdated;
        
        private void LessonUpdated(LessonUpdatedEventDTO lessonUpdated)
        {
            OnLessonUpdated?.Invoke(lessonUpdated);
        }


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

			await EventManager.Unsubscribe<LessonRegisteredEventDTO>(LessonRegistered, ContractAddress);
			OnLessonRegistered = null;
			await EventManager.Unsubscribe<LessonUpdatedEventDTO>(LessonUpdated, ContractAddress);
			OnLessonUpdated = null;

            
            
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

                await EventManager.Subscribe<LessonRegisteredEventDTO>(LessonRegistered, ContractAddress);
                await EventManager.Subscribe<LessonUpdatedEventDTO>(LessonUpdated, ContractAddress);
    
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
