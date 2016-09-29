using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSupervisorForModel
{
    static class DataCollectionLibrary
    {
        static internal List<OptionSpreadExpression> optionSpreadExpressionList = new List<OptionSpreadExpression>();

        static internal ConcurrentDictionary<string, OptionSpreadExpression> optionSpreadExpressionHashTable_keySymbol
            = new ConcurrentDictionary<string, OptionSpreadExpression>();

        static internal ConcurrentDictionary<string, OptionSpreadExpression> optionSpreadExpressionHashTable_keyCQGInId
            = new ConcurrentDictionary<string, OptionSpreadExpression>();

        static internal ConcurrentDictionary<string, OptionSpreadExpression> optionSpreadExpressionHashTable_keyFullName
            = new ConcurrentDictionary<string, OptionSpreadExpression>();

        static internal Dictionary<long, OptionSpreadExpression> optionSpreadExpressionHashTable_keycontractId
            = new Dictionary<long, OptionSpreadExpression>();

        static internal Dictionary<long, Instrument> instrumentHashTable
            = new Dictionary<long, Instrument>();

        static internal Dictionary<long, List<Contract>> contractHashTableByInstId
            = new Dictionary<long, List<Contract>>();

        static internal List<Instrument> instrumentList = new List<Instrument>();

        static internal DataTable contractSummaryGridList = new DataTable();

    }
}
