using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ServiceLib.ViewModels
{
    public class ProfilesViewModel : MyReactiveObject
    {
        #region private prop

        private List<ProfileItem> _lstProfile;
        private string _serverFilter = string.Empty;
        private Dictionary<string, bool> _dicHeaderSort = new();

        #endregion private prop

        #region ObservableCollection

        public IObservableCollection<ProfileItemModel> _profileItems = new ObservableCollectionExtended<ProfileItemModel>();
        public IObservableCollection<ProfileItemModel> ProfileItems => _profileItems;

        private IObservableCollection<SubItem> _subItems = new ObservableCollectionExtended<SubItem>();
        public IObservableCollection<SubItem> SubItems => _subItems;

        private IObservableCollection<ComboItem> _servers = new ObservableCollectionExtended<ComboItem>();

        [Reactive]
        public ProfileItemModel SelectedProfile { get; set; }

        public IList<ProfileItemModel> SelectedProfiles { get; set; }

        [Reactive]
        public SubItem SelectedSub { get; set; }

        [Reactive]
        public SubItem SelectedMoveToGroup { get; set; }

        [Reactive]
        public ComboItem SelectedServer { get; set; }

        [Reactive]
        public string ServerFilter { get; set; }

        [Reactive]
        public bool BlServers { get; set; }

        #endregion ObservableCollection

        #region Menu

        //servers delete
        public ReactiveCommand<Unit, Unit> EditServerCmd { get; }

        public ReactiveCommand<Unit, Unit> RemoveServerCmd { get; }
        public ReactiveCommand<Unit, Unit> RemoveDuplicateServerCmd { get; }
        public ReactiveCommand<Unit, Unit> CopyServerCmd { get; }
        public ReactiveCommand<Unit, Unit> SetDefaultServerCmd { get; }
        public ReactiveCommand<Unit, Unit> ShareServerCmd { get; }
        public ReactiveCommand<Unit, Unit> SetDefaultMultipleServerCmd { get; }
        public ReactiveCommand<Unit, Unit> SetDefaultLoadBalanceServerCmd { get; }

        //servers move
        public ReactiveCommand<Unit, Unit> MoveTopCmd { get; }

        public ReactiveCommand<Unit, Unit> MoveUpCmd { get; }
        public ReactiveCommand<Unit, Unit> MoveDownCmd { get; }
        public ReactiveCommand<Unit, Unit> MoveBottomCmd { get; }

        //servers ping
        public ReactiveCommand<Unit, Unit> MixedTestServerCmd { get; }

        public ReactiveCommand<Unit, Unit> TcpingServerCmd { get; }
        public ReactiveCommand<Unit, Unit> RealPingServerCmd { get; }
        public ReactiveCommand<Unit, Unit> SpeedServerCmd { get; }
        public ReactiveCommand<Unit, Unit> SortServerResultCmd { get; }

        //servers export
        public ReactiveCommand<Unit, Unit> Export2ClientConfigCmd { get; }

        public ReactiveCommand<Unit, Unit> Export2ClientConfigClipboardCmd { get; }
        public ReactiveCommand<Unit, Unit> Export2ShareUrlCmd { get; }
        public ReactiveCommand<Unit, Unit> Export2ShareUrlBase64Cmd { get; }

        public ReactiveCommand<Unit, Unit> AddSubCmd { get; }
        public ReactiveCommand<Unit, Unit> EditSubCmd { get; }

        #endregion Menu

        #region Init

        public ProfilesViewModel(Func<EViewAction, object?, Task<bool>>? updateView)
        {
            _config = AppHandler.Instance.Config;
            _updateView = updateView;

            #region WhenAnyValue && ReactiveCommand

            var canEditRemove = this.WhenAnyValue(
               x => x.SelectedProfile,
               selectedSource => selectedSource != null && !selectedSource.IndexId.IsNullOrEmpty());

            this.WhenAnyValue(
                x => x.SelectedSub,
                y => y != null && !y.Remarks.IsNullOrEmpty() && _config.SubIndexId != y.Id)
                    .Subscribe(async c => await SubSelectedChangedAsync(c));
            this.WhenAnyValue(
                 x => x.SelectedMoveToGroup,
                 y => y != null && !y.Remarks.IsNullOrEmpty())
                     .Subscribe(async c => await MoveToGroup(c));

            this.WhenAnyValue(
              x => x.SelectedServer,
              y => y != null && !y.Text.IsNullOrEmpty())
                  .Subscribe(async c => await ServerSelectedChanged(c));

            this.WhenAnyValue(
              x => x.ServerFilter,
              y => y != null && _serverFilter != y)
                  .Subscribe(c => ServerFilterChanged(c));

            //servers delete
            EditServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await EditServerAsync(EConfigType.Custom);
            }, canEditRemove);
            RemoveServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await RemoveServerAsync();
            }, canEditRemove);
            RemoveDuplicateServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await RemoveDuplicateServer();
            });
            CopyServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await CopyServer();
            }, canEditRemove);
            SetDefaultServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await SetDefaultServer();
            }, canEditRemove);
            ShareServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ShareServerAsync();
            }, canEditRemove);
            SetDefaultMultipleServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await SetDefaultMultipleServer(ECoreType.sing_box);
            }, canEditRemove);
            SetDefaultLoadBalanceServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await SetDefaultMultipleServer(ECoreType.Xray);
            }, canEditRemove);

            //servers move
            MoveTopCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await MoveServer(EMove.Top);
            }, canEditRemove);
            MoveUpCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await MoveServer(EMove.Up);
            }, canEditRemove);
            MoveDownCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await MoveServer(EMove.Down);
            }, canEditRemove);
            MoveBottomCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await MoveServer(EMove.Bottom);
            }, canEditRemove);

            //servers ping
            MixedTestServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ServerSpeedtest(ESpeedActionType.Mixedtest);
            });
            TcpingServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ServerSpeedtest(ESpeedActionType.Tcping);
            }, canEditRemove);
            RealPingServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ServerSpeedtest(ESpeedActionType.Realping);
            }, canEditRemove);
            SpeedServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ServerSpeedtest(ESpeedActionType.Speedtest);
            }, canEditRemove);
            SortServerResultCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await SortServer(EServerColName.DelayVal.ToString());
            });
            //servers export
            Export2ClientConfigCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await Export2ClientConfigAsync(false);
            }, canEditRemove);
            Export2ClientConfigClipboardCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await Export2ClientConfigAsync(true);
            }, canEditRemove);
            Export2ShareUrlCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await Export2ShareUrlAsync(false);
            }, canEditRemove);
            Export2ShareUrlBase64Cmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await Export2ShareUrlAsync(true);
            }, canEditRemove);

            //Subscription
            AddSubCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await EditSubAsync(true);
            });
            EditSubCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await EditSubAsync(false);
            });

            #endregion WhenAnyValue && ReactiveCommand

            if (_updateView != null)
            {
                MessageBus.Current.Listen<string>(EMsgCommand.RefreshProfiles.ToString()).Subscribe(OnNext);
            }

            _ = Init();
        }

        private async Task Init()
        {
            SelectedProfile = new();
            SelectedSub = new();
            SelectedMoveToGroup = new();
            SelectedServer = new();

            await RefreshSubscriptions();
            RefreshServers();
        }

        #endregion Init

        #region Actions

        private async void OnNext(string x)
        {
            await _updateView?.Invoke(EViewAction.DispatcherRefreshServersBiz, null);
        }

        private void Reload()
        {
            Locator.Current.GetService<MainWindowViewModel>()?.Reload();
        }

        public void UpdateSpeedtestHandler(SpeedTestResult result)
        {
            _updateView?.Invoke(EViewAction.DispatcherSpeedTest, result);
        }

        public void SetSpeedTestResult(SpeedTestResult result)
        {
            if (Utils.IsNullOrEmpty(result.IndexId))
            {
                NoticeHandler.Instance.SendMessageEx(result.Delay);
                NoticeHandler.Instance.Enqueue(result.Delay);
                return;
            }
            var item = _profileItems.Where(it => it.IndexId == result.IndexId).FirstOrDefault();
            if (item != null)
            {
                if (Utils.IsNotEmpty(result.Delay))
                {
                    int.TryParse(result.Delay, out int temp);
                    item.Delay = temp;
                    item.DelayVal = $"{result.Delay} {Global.DelayUnit}";
                }
                if (Utils.IsNotEmpty(result.Speed))
                {
                    item.SpeedVal = $"{result.Speed} {Global.SpeedUnit}";
                }
                _profileItems.Replace(item, JsonUtils.DeepCopy(item));
            }
        }

        public void UpdateStatistics(ServerSpeedItem update)
        {
            try
            {
                var item = _profileItems.Where(it => it.IndexId == update.IndexId).FirstOrDefault();
                if (item != null)
                {
                    item.TodayDown = Utils.HumanFy(update.TodayDown);
                    item.TodayUp = Utils.HumanFy(update.TodayUp);
                    item.TotalDown = Utils.HumanFy(update.TotalDown);
                    item.TotalUp = Utils.HumanFy(update.TotalUp);

                    if (SelectedProfile?.IndexId == item.IndexId)
                    {
                        var temp = JsonUtils.DeepCopy(item);
                        _profileItems.Replace(item, temp);
                        SelectedProfile = temp;
                    }
                    else
                    {
                        _profileItems.Replace(item, JsonUtils.DeepCopy(item));
                    }
                }
            }
            catch
            {
            }
        }

        public async Task AutofitColumnWidthAsync()
        {
            await _updateView?.Invoke(EViewAction.AdjustMainLvColWidth, null);
        }

        #endregion Actions

        #region Servers && Groups

        private async Task SubSelectedChangedAsync(bool c)
        {
            if (!c)
            {
                return;
            }
            _config.SubIndexId = SelectedSub?.Id;

            RefreshServers();

            await _updateView?.Invoke(EViewAction.ProfilesFocus, null);
        }

        private void ServerFilterChanged(bool c)
        {
            if (!c)
            {
                return;
            }
            _serverFilter = ServerFilter;
            if (Utils.IsNullOrEmpty(_serverFilter))
            {
                RefreshServers();
            }
        }

        public void RefreshServers()
        {
            MessageBus.Current.SendMessage("", EMsgCommand.RefreshProfiles.ToString());
        }

        public async Task RefreshServersBiz()
        {
            
            var lstModel = await GetProfileItemsEx(_config.SubIndexId, _serverFilter);
            _lstProfile = JsonUtils.Deserialize<List<ProfileItem>>(JsonUtils.Serialize(lstModel)) ?? [];

            _profileItems.Clear();
            _profileItems.AddRange(lstModel);
            if (lstModel.Count > 0)
            {
                var selected = lstModel.FirstOrDefault(t => t.IndexId == _config.IndexId);
                if (selected != null)
                {
                    SelectedProfile = selected;
                }
                else
                {
                    SelectedProfile = lstModel.First();
                }
            }
        }

        public async Task RefreshSubscriptions()
        {
            _subItems.Clear();

            _subItems.Add(new SubItem { Remarks = ResUI.AllGroupServers });

            foreach (var item in await AppHandler.Instance.SubItems())
            {
                _subItems.Add(item);
            }
            if (_config.SubIndexId != null && _subItems.FirstOrDefault(t => t.Id == _config.SubIndexId) != null)
            {
                SelectedSub = _subItems.FirstOrDefault(t => t.Id == _config.SubIndexId);
            }
            else
            {
                SelectedSub = _subItems.First();
            }
        }

        public async Task<List<ProfileItemModel>?> GetProfileItemsEx(string subid="", string filter="")
        {
            var lstModel = await AppHandler.Instance.ProfileItems(_config.SubIndexId, filter);

            await ConfigHandler.SetDefaultServer(_config, lstModel);

            var lstServerStat = (_config.GuiItem.EnableStatistics ? StatisticsHandler.Instance.ServerStat : null) ?? [];
            var lstProfileExs = await ProfileExHandler.Instance.GetProfileExs();
            lstModel = (from t in lstModel
                        join t2 in lstServerStat on t.IndexId equals t2.IndexId into t2b
                        from t22 in t2b.DefaultIfEmpty()
                        join t3 in lstProfileExs on t.IndexId equals t3.IndexId into t3b
                        from t33 in t3b.DefaultIfEmpty()
                        select new ProfileItemModel
                        {
                            IndexId = t.IndexId,
                            ConfigType = t.ConfigType,
                            Remarks = t.Remarks,
                            Address = t.Address,
                            Port = t.Port,
                            Security = t.Security,
                            Network = t.Network,
                            StreamSecurity = t.StreamSecurity,
                            Subid = t.Subid,
                            SubRemarks = t.SubRemarks,
                            IsActive = t.IndexId == _config.IndexId,
                            Sort = t33 == null ? 0 : t33.Sort,
                            Delay = t33 == null ? 0 : t33.Delay,
                            DelayVal = t33?.Delay != 0 ? $"{t33?.Delay} {Global.DelayUnit}" : string.Empty,
                            SpeedVal = t33?.Speed != 0 ? $"{t33?.Speed} {Global.SpeedUnit}" : string.Empty,
                            TodayDown = t22 == null ? "" : Utils.HumanFy(t22.TodayDown),
                            TodayUp = t22 == null ? "" : Utils.HumanFy(t22.TodayUp),
                            TotalDown = t22 == null ? "" : Utils.HumanFy(t22.TotalDown),
                            TotalUp = t22 == null ? "" : Utils.HumanFy(t22.TotalUp),
                            DateNow= t22 == null ? 0 : t22.DateNow
                        }).OrderBy(t => t.Sort).ToList();

            return lstModel;
        }
        public async Task<ProfileItemModel> GetProfileItemsExByindex(string SelectedindexId)
        {
            if (SelectedindexId == "")
                return null;
            var lstModel = await AppHandler.Instance.ProfileItems("", "");

            await ConfigHandler.SetDefaultServer(_config, lstModel);

            var lstServerStat = (_config.GuiItem.EnableStatistics ? StatisticsHandler.Instance.ServerStat : null) ?? [];
            var lstProfileExs = await ProfileExHandler.Instance.GetProfileExs();
            lstModel = (from t in lstModel
                        join t2 in lstServerStat on t.IndexId equals t2.IndexId into t2b
                        from t22 in t2b.DefaultIfEmpty()
                        join t3 in lstProfileExs on t.IndexId equals t3.IndexId into t3b
                        from t33 in t3b.DefaultIfEmpty()
                        select new ProfileItemModel
                        {
                            Id=t.Id,
                            IndexId = t.IndexId,
                            ConfigType = t.ConfigType,
                            Remarks = t.Remarks,
                            Address = t.Address,
                            Port = t.Port,
                            Security = t.Security,
                            Network = t.Network,
                            StreamSecurity = t.StreamSecurity,
                            Subid = t.Subid,
                            SubRemarks = t.SubRemarks,
                            IsActive = t.IndexId == _config.IndexId,
                            Sort = t33 == null ? 0 : t33.Sort,
                            Delay = t33 == null ? 0 : t33.Delay,
                            DelayVal = t33?.Delay != 0 ? $"{t33?.Delay} {Global.DelayUnit}" : string.Empty,
                            SpeedVal = t33?.Speed != 0 ? $"{t33?.Speed} {Global.SpeedUnit}" : string.Empty,
                            TodayDown = t22 == null ? "" : Utils.HumanFy(t22.TodayDown),
                            TodayUp = t22 == null ? "" : Utils.HumanFy(t22.TodayUp),
                            TotalDown = t22 == null ? "" : Utils.HumanFy(t22.TotalDown),
                            TotalUp = t22 == null ? "" : Utils.HumanFy(t22.TotalUp),
                            DateNow = t22 == null ? 0 : t22.DateNow
                        }).OrderBy(t => t.Sort).ToList();
            lstModel=lstModel.Where(st=> st.Id==SelectedindexId).OrderBy(t => t.Sort).ToList();
            return (ProfileItemModel)lstModel.FirstOrDefault();
        }

        #endregion Servers && Groups

        #region Add Servers

        public async Task<List<ProfileItem>?> GetProfileItems(bool latest)
        {
            var lstSelecteds = new List<ProfileItem>();
            if (SelectedProfiles == null || SelectedProfiles.Count <= 0)
            {
                return null;
            }

            var orderProfiles = SelectedProfiles?.OrderBy(t => t.Sort);
            if (latest)
            {
                foreach (var profile in orderProfiles)
                {
                    var item = await AppHandler.Instance.GetProfileItem(profile.IndexId);
                    if (item is not null)
                    {
                        lstSelecteds.Add(item);
                    }
                }
            }
            else
            {
                lstSelecteds = JsonUtils.Deserialize<List<ProfileItem>>(JsonUtils.Serialize(orderProfiles));
            }

            return lstSelecteds;
        }
        /// <summary>
        /// get profile of itemby index id
        /// </summary>
        /// <param name="latest"></param>
        /// <returns></returns>
        public async Task<List<ProfileItem>?> GetProfileItemsBySelected(ProfileItemModel newprfItem)
        {
            var lstSelecteds = new List<ProfileItem>();
            if (newprfItem == null)
                return null;
            var item = await AppHandler.Instance.GetProfileItem(newprfItem.IndexId);
            if (item is not null)
            {
                lstSelecteds.Add(item);
            }

            return lstSelecteds;
        }
        public async Task<List<ProfileItem>?> GetAllProfileItems()
        {
            var lstSelecteds  = await AppHandler.Instance.GetAllProfileItem();
            return lstSelecteds;
        }

        public async Task EditServerAsync(EConfigType eConfigType)
        {
            if (Utils.IsNullOrEmpty(SelectedProfile?.IndexId))
            {
                return;
            }
            var item = await AppHandler.Instance.GetProfileItem(SelectedProfile.IndexId);
            if (item is null)
            {
                NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectServer);
                return;
            }
            eConfigType = item.ConfigType;

            bool? ret = false;
            if (eConfigType == EConfigType.Custom)
            {
                ret = await _updateView?.Invoke(EViewAction.AddServer2Window, item);
            }
            else
            {
                ret = await _updateView?.Invoke(EViewAction.AddServerWindow, item);
            }
            if (ret == true)
            {
                RefreshServers();
                if (item.IndexId == _config.IndexId)
                {
                    Reload();
                }
            }
        }
        public async Task RemoveServersAsync()
        {
            var lstSelecteds = await GetAllProfileItems();
            if (lstSelecteds == null)
            {
                return;
            }
            //if (await _updateView?.Invoke(EViewAction.ShowYesNo, null) == false)
            //{
            //    return;
            //}
            //var exists = lstSelecteds.Exists(t => t.IndexId == _config.IndexId);

            await ConfigHandler.RemoveServer(_config, lstSelecteds);
            NoticeHandler.Instance.Enqueue(ResUI.RemoveTimelyAutomated);
            if (lstSelecteds.Count == _profileItems.Count)
            {
                _profileItems.Clear();
            }
            RefreshServers();
            Reload();

            //if (exists)
            //{
            //    Reload();
            //}
        }
        public async Task RemoveServerAsync()
        {
            var lstSelecteds = await GetProfileItems(true);
            if (lstSelecteds == null)
            {
                return;
            }
            if (await _updateView?.Invoke(EViewAction.ShowYesNo, null) == false)
            {
                return;
            }
            var exists = lstSelecteds.Exists(t => t.IndexId == _config.IndexId);

            await ConfigHandler.RemoveServer(_config, lstSelecteds);
            NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
            if (lstSelecteds.Count == _profileItems.Count)
            {
                _profileItems.Clear();
            }
            RefreshServers();
            if (exists)
            {
                Reload();
            }
        }

        public async Task RemoveDuplicateServer()
        {
            var tuple = await ConfigHandler.DedupServerList(_config, _config.SubIndexId);
            //if (!await isDuplicateServer())
            //    return;
            RefreshServers();
            Reload();
            NoticeHandler.Instance.Enqueue(string.Format(ResUI.RemoveDuplicateServerResult, tuple.Item1, tuple.Item2));
        }
        public async Task<bool> isDuplicateServer()
        {
            var tuple = await ConfigHandler.DedupServerList(_config, _config.SubIndexId);
            if (tuple == null | tuple?.Item1 <2)
                return false;
            else
                return true;
        }

        private async Task CopyServer()
        {
            var lstSelecteds = await GetProfileItems(false);
            if (lstSelecteds == null)
            {
                return;
            }
            if (await ConfigHandler.CopyServer(_config, lstSelecteds) == 0)
            {
                RefreshServers();
                NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
            }
        }

        public async Task SetDefaultServer()
        {
            if (Utils.IsNullOrEmpty(SelectedProfile?.IndexId))
            {
                return;
            }
            await SetDefaultServer(SelectedProfile.IndexId);
        }

        public async Task SetDefaultServer(string indexId)
        {
            if (Utils.IsNullOrEmpty(indexId))
            {
                return;
            }
            if (indexId == _config.IndexId)
            {
                return;
            }
            var item = await AppHandler.Instance.GetProfileItem(indexId);
            if (item is null)
            {
                NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectServer);
                return;
            }

            if (await ConfigHandler.SetDefaultServerIndex(_config, indexId) == 0)
            {
                RefreshServers();
                Reload();
            }
        }

        private async Task ServerSelectedChanged(bool c)
        {
            if (!c)
            {
                return;
            }
            if (SelectedServer == null)
            {
                return;
            }
            if (Utils.IsNullOrEmpty(SelectedServer.ID))
            {
                return;
            }
            await SetDefaultServer(SelectedServer.ID);
        }

        public async Task ShareServerAsync()
        {
            var item = await AppHandler.Instance.GetProfileItem(SelectedProfile.IndexId);
            if (item is null)
            {
                NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectServer);
                return;
            }
            var url = FmtHandler.GetShareUri(item);
            if (Utils.IsNullOrEmpty(url))
            {
                return;
            }

            await _updateView?.Invoke(EViewAction.ShareServer, url);
        }

        private async Task SetDefaultMultipleServer(ECoreType coreType)
        {
            var lstSelecteds = await GetProfileItems(true);
            if (lstSelecteds == null)
            {
                return;
            }

            var ret = await ConfigHandler.AddCustomServer4Multiple(_config, lstSelecteds, coreType);
            if (ret.Success != true)
            {
                NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
                return;
            }
            if (ret?.Data?.ToString() == _config.IndexId)
            {
                RefreshServers();
                Reload();
            }
            else
            {
                await SetDefaultServer(ret?.Data?.ToString());
            }
        }

        public async Task SortServer(string colName)
        {
            if (Utils.IsNullOrEmpty(colName))
            {
                return;
            }

            _dicHeaderSort.TryAdd(colName, true);
            _dicHeaderSort.TryGetValue(colName, out bool asc);
            if (await ConfigHandler.SortServers(_config, _config.SubIndexId, colName, asc) != 0)
            {
                return;
            }
            _dicHeaderSort[colName] = !asc;
            RefreshServers();
        }

        //move server
        private async Task MoveToGroup(bool c)
        {
            if (!c)
            {
                return;
            }

            var lstSelecteds = await GetProfileItems(true);
            if (lstSelecteds == null)
            {
                return;
            }

            await ConfigHandler.MoveToGroup(_config, lstSelecteds, SelectedMoveToGroup.Id);
            NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);

            RefreshServers();
            SelectedMoveToGroup = null;
            SelectedMoveToGroup = new();
            //Reload();
        }

        public async Task MoveServer(EMove eMove)
        {
            var item = _lstProfile.FirstOrDefault(t => t.IndexId == SelectedProfile.IndexId);
            if (item is null)
            {
                NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectServer);
                return;
            }

            int index = _lstProfile.IndexOf(item);
            if (index < 0)
            {
                return;
            }
            if (await ConfigHandler.MoveServer(_config, _lstProfile, index, eMove) == 0)
            {
                RefreshServers();
            }
        }

        public async Task MoveServerTo(int startIndex, ProfileItemModel targetItem)
        {
            var targetIndex = _profileItems.IndexOf(targetItem);
            if (startIndex >= 0 && targetIndex >= 0 && startIndex != targetIndex)
            {
                if (await ConfigHandler.MoveServer(_config, _lstProfile, startIndex, EMove.Position, targetIndex) == 0)
                {
                    RefreshServers();
                }
            }
        }

        public async Task ServerSpeedtest(ESpeedActionType actionType)
        {
            if (actionType == ESpeedActionType.Mixedtest)
            {
                SelectedProfiles = _profileItems;
            }

            var lstSelecteds = await GetProfileItems(false);
            if (lstSelecteds == null)
            {
                return;
            }
            //ClearTestResult();

            _ = new SpeedtestService(_config, lstSelecteds, actionType, UpdateSpeedtestHandler);
        }
        /// <summary>
        /// get speed by send profile item
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="NewPrfItem"></param>
        /// <returns></returns>
        public async Task ServerSpeedtestByselected(ESpeedActionType actionType, ProfileItemModel NewPrfItem)
        {

            ////var prfitem = GetProfileItemsExByindex(id);
            ////SelectedProfiles = prfitem.Result;
            ////var lstSelecteds = await GetProfileItems(false);
            var lstSelecteds = await GetProfileItemsBySelected(NewPrfItem);
            if (lstSelecteds == null)
            {
                return;
            }
            //ClearTestResult();

            _ = new SpeedtestService(_config, lstSelecteds, actionType, UpdateSpeedtestHandler);
        }
        public void ServerSpeedtestStop()
        {
            MessageBus.Current.SendMessage("", EMsgCommand.StopSpeedtest.ToString());
        }

        private async Task Export2ClientConfigAsync(bool blClipboard)
        {
            var item = await AppHandler.Instance.GetProfileItem(SelectedProfile.IndexId);
            if (item is null)
            {
                NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectServer);
                return;
            }
            if (blClipboard)
            {
                var result = await CoreConfigHandler.GenerateClientConfig(item, null);
                if (result.Success != true)
                {
                    NoticeHandler.Instance.Enqueue(result.Msg);
                }
                else
                {
                    await _updateView?.Invoke(EViewAction.SetClipboardData, result.Data);
                    NoticeHandler.Instance.SendMessage(ResUI.OperationSuccess);
                }
            }
            else
            {
                await _updateView?.Invoke(EViewAction.SaveFileDialog, item);
            }
        }

        public async Task Export2ClientConfigResult(string fileName, ProfileItem item)
        {
            if (Utils.IsNullOrEmpty(fileName))
            {
                return;
            }
            var result = await CoreConfigHandler.GenerateClientConfig(item, fileName);
            if (result.Success != true)
            {
                NoticeHandler.Instance.Enqueue(result.Msg);
            }
            else
            {
                NoticeHandler.Instance.SendMessageAndEnqueue(string.Format(ResUI.SaveClientConfigurationIn, fileName));
            }
        }

        public async Task Export2ShareUrlAsync(bool blEncode)
        {
            var lstSelecteds = await GetProfileItems(true);
            if (lstSelecteds == null)
            {
                return;
            }

            StringBuilder sb = new();
            foreach (var it in lstSelecteds)
            {
                var url = FmtHandler.GetShareUri(it);
                if (Utils.IsNullOrEmpty(url))
                {
                    continue;
                }
                sb.Append(url);
                sb.AppendLine();
            }
            if (sb.Length > 0)
            {
                if (blEncode)
                {
                    await _updateView?.Invoke(EViewAction.SetClipboardData, Utils.Base64Encode(sb.ToString()));
                }
                else
                {
                    await _updateView?.Invoke(EViewAction.SetClipboardData, sb.ToString());
                }
                NoticeHandler.Instance.SendMessage(ResUI.BatchExportURLSuccessfully);
            }
        }

        #endregion Add Servers

        #region Subscription

        private async Task EditSubAsync(bool blNew)
        {
            SubItem item;
            if (blNew)
            {
                item = new();
            }
            else
            {
                item = await AppHandler.Instance.GetSubItem(_config.SubIndexId);
                if (item is null)
                {
                    return;
                }
            }
            if (await _updateView?.Invoke(EViewAction.SubEditWindow, item) == true)
            {
                await RefreshSubscriptions();
                await SubSelectedChangedAsync(true);
            }
        }

        #endregion Subscription
    }
}
