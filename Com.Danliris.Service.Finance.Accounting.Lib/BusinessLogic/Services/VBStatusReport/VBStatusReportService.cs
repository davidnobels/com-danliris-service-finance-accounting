﻿using Com.Danliris.Service.Finance.Accounting.Lib.Helpers;
using Com.Danliris.Service.Finance.Accounting.Lib.Services.IdentityService;
using Com.Danliris.Service.Finance.Accounting.Lib.ViewModels.VBStatusReport;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Com.Danliris.Service.Finance.Accounting.Lib.ViewModels.NewIntegrationViewModel;
using Com.Danliris.Service.Finance.Accounting.Lib.BusinessLogic.Interfaces.VBStatusReport;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Com.Moonlay.Models;

namespace Com.Danliris.Service.Finance.Accounting.Lib.BusinessLogic.Services.VBStatusReport
{
    public class VBStatusReportService : IVBStatusReportService
    {
        private const string _UserAgent = "finance-service";
        protected DbSet<VbRequestModel> _DbSet;
        protected DbSet<RealizationVbModel> _RealizationDbSet;
        private readonly IServiceProvider _serviceProvider;
        protected IIdentityService _IdentityService;
        public FinanceDbContext _DbContext;

        public VBStatusReportService(IServiceProvider serviceProvider, FinanceDbContext dbContext)
        {
            _DbContext = dbContext;
            _DbSet = dbContext.Set<VbRequestModel>();
            _RealizationDbSet = dbContext.Set<RealizationVbModel>();
            _serviceProvider = serviceProvider;
            _IdentityService = serviceProvider.GetService<IIdentityService>();
        }

        private async Task<List<VBStatusReportViewModel>> GetReportQuery(int unitId, long vbRequestId, bool? isRealized, DateTimeOffset? requestDateFrom, DateTimeOffset? requestDateTo, DateTimeOffset? realizeDateFrom, DateTimeOffset? realizeDateTo, int offSet)
        {
            var query = _DbSet.AsQueryable();

            if (unitId != 0)
            {
                query = query.Where(s => s.UnitId == unitId);
            }

            if (vbRequestId != 0)
            {
                query = query.Where(s => s.Id == vbRequestId);
            }

            if (isRealized.HasValue)
            {
                query = query.Where(s => s.Complete_Status == isRealized.GetValueOrDefault());
            }

            if (requestDateFrom.HasValue && requestDateTo.HasValue)
            {
                query = query.Where(s => requestDateFrom.Value.Date <= s.Date.AddHours(offSet).Date && s.Date.AddHours(offSet).Date <= requestDateTo.Value.Date);
            }
            else if (requestDateFrom.HasValue && !requestDateTo.HasValue)
            {
                query = query.Where(s => requestDateFrom.Value.Date <= s.Date.AddHours(offSet).Date);
            }
            else if (!requestDateFrom.HasValue && requestDateTo.HasValue)
            {
                query = query.Where(s => s.Date.AddHours(offSet).Date <= requestDateTo.Value.Date);
            }

            var realizationQuery = _RealizationDbSet.AsQueryable();

            if (realizeDateFrom.HasValue && realizeDateTo.HasValue)
            {
                realizationQuery = realizationQuery.Where(s => realizeDateFrom.Value.Date <= s.Date.AddHours(offSet).Date && s.Date.AddHours(offSet).Date <= realizeDateTo.Value.Date);
            }
            else if (realizeDateFrom.HasValue && !realizeDateTo.HasValue)
            {
                realizationQuery = realizationQuery.Where(s => realizeDateFrom.Value.Date <= s.Date.AddHours(offSet).Date);
            }
            else if (!realizeDateFrom.HasValue && realizeDateTo.HasValue)
            {
                realizationQuery = realizationQuery.Where(s => s.Date.AddHours(offSet).Date <= realizeDateTo.Value.Date);
            }

            var result = query
                .Join(realizationQuery,
                (rqst)=> rqst.VBNo,
                (real)=> real.VBNo,
                (rqst,real)=> new VBStatusReportViewModel(){
                    Id = rqst.Id,
                    VBNo = rqst.VBNo,
                    Date = rqst.Date,
                    DateEstimate = real.DateEstimate,
                    Unit = new Unit()
                    {
                        Id = rqst.Id,
                        Name = rqst.UnitName,
                    },
                    CreateBy = rqst.CreatedBy,
                    RealizationNo = real.VBNoRealize,
                    RealizationDate = real.Date,
                    Usage = rqst.Usage,
                    Aging = (int)(real.DateEstimate-real.Date).TotalDays,
                    Amount = rqst.Amount,
                    RealizationAmount = real.Amount,
                    Difference = rqst.Amount - real.Amount,
                    Status = real.isVerified ? "Realisasi" : "Outstanding",
                    LastModifiedUtc = real.LastModifiedUtc,
                })
                .OrderByDescending(s => s.LastModifiedUtc).ToList();

            return result.ToList();
        }

        public async Task<List<VBStatusReportViewModel>> GetReport(int unitId, long vbRequestId, bool? isRealized, DateTimeOffset? requestDateFrom, DateTimeOffset? requestDateTo, DateTimeOffset? realizeDateFrom, DateTimeOffset? realizeDateTo, int offSet)
        {
            var data = await GetReportQuery(unitId, vbRequestId, isRealized, requestDateFrom, requestDateTo, realizeDateFrom, realizeDateTo, offSet);

            return data;
        }

        public async Task<MemoryStream> GenerateExcel(int unitId, long vbRequestId, bool? isRealized, DateTimeOffset? requestDateFrom, DateTimeOffset? requestDateTo, DateTimeOffset? realizeDateFrom, DateTimeOffset? realizeDateTo, int offSet)
        {
            var data = await GetReportQuery(unitId, vbRequestId, isRealized, requestDateFrom, requestDateTo, realizeDateFrom, realizeDateTo, offSet);

            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn() { ColumnName = "No VB", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Tanggal VB", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Estimasi Tgl Realisasi", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Unit", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Pemohon VB", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "No Realisasi", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Tgl Realisasi", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Keperluan VB", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Aging (Hari)", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Jumlah VB", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Realisasi", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Sisa (Kurang/Lebih)", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn() { ColumnName = "Status", DataType = typeof(string) });

            if (data.Count == 0)
            {
                dt.Rows.Add("", "", "", "", "", "", "", "", "", "", "", "", "");
            }
            else
            {
                data = data.OrderBy(s => s.Id).ToList();
                foreach (var item in data)
                {
                    dt.Rows.Add(item.VBNo, item.Date.ToOffset(new TimeSpan(offSet, 0, 0)).ToString("d/M/yyyy", new CultureInfo("id-ID")), 
                        item.DateEstimate.ToOffset(new TimeSpan(offSet, 0, 0)).ToString("d/M/yyyy", new CultureInfo("id-ID")), item.Unit.Name, item.CreateBy, item.RealizationNo, 
                        item.RealizationDate.ToOffset(new TimeSpan(offSet, 0, 0)).ToString("d/M/yyyy", new CultureInfo("id-ID")), 
                        item.Usage, item.Aging, item.Amount, item.RealizationAmount, item.Difference, item.Status);
                }
            }

            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(dt, "Status VB") }, true);
        }

        public Task<int> CreateAsync(VbRequestModel model)
        {
            EntityExtension.FlagForCreate(model, _IdentityService.Username, _UserAgent);

            _DbContext.VbRequests.Add(model);

            return _DbContext.SaveChangesAsync();
        }

        public Task<VbRequestModel> ReadByIdAsync(int id)
        {
            return _DbContext.VbRequests.Where(entity => entity.Id == id).FirstOrDefaultAsync();
        }
    }
}