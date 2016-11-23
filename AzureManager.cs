using Contoso_Bank.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contoso_Bank
{
    public class AzureManager
    {

        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<xizhesContosoBank> xizhesContosoBankTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("http://xizhescontosobank.azurewebsites.net");
            this.xizhesContosoBankTable = this.client.GetTable<xizhesContosoBank>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task AddxizhesContosoBank(xizhesContosoBank azureObject)
        {
            await this.xizhesContosoBankTable.InsertAsync(azureObject);
        }

        public async Task<List<xizhesContosoBank>> GetxizhesContosoBanks()
        {
            return await this.xizhesContosoBankTable.ToListAsync();
        }

        public async Task UpdatexizhesContosoBank(xizhesContosoBank xizhesContosoBank)
        {
            await this.xizhesContosoBankTable.UpdateAsync(xizhesContosoBank);
        }

        public async Task DeletexizhesContosoBank(xizhesContosoBank xizhesContosoBank)
        {
            await this.xizhesContosoBankTable.DeleteAsync(xizhesContosoBank);
        }
    }
}
