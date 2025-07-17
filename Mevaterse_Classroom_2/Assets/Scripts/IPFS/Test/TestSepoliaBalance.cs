using UnityEngine;
using Nethereum.Web3;
using System.Threading.Tasks;

public class TestSepoliaBalance : MonoBehaviour
{
    public string rpcUrl = "https://ethereum-sepolia.core.chainstack.com/TUO_ID"; // tuo endpoint
    public string walletAddress = "QUI_METTI_L'INDIRIZZO_CHE_LEGGI_DAL_WALLET";

    async void Start()
    {
        var web3 = new Web3(rpcUrl);
        var balance = await web3.Eth.GetBalance.SendRequestAsync(walletAddress);
        Debug.Log("Saldo Sepolia: " + Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value));
    }
}
