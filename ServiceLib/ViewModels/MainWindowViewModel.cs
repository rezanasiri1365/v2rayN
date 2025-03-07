using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System.Net.Http;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http.Headers;
using static QRCoder.PayloadGenerator;
using System.Text;
using ServiceLib.Models;
using DynamicData.Binding;

namespace ServiceLib.ViewModels
{
    public class MainWindowViewModel : MyReactiveObject
    {
        #region Menu

        //servers
        public ReactiveCommand<Unit, Unit> AddVmessServerCmd { get; }

        public ReactiveCommand<Unit, Unit> AddVlessServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddShadowsocksServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddSocksServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddHttpServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddTrojanServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddHysteria2ServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddTuicServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddWireguardServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddCustomServerCmd { get; }
        public ReactiveCommand<Unit, Unit> AddServerViaClipboardCmd { get; }
        public ReactiveCommand<Unit, Unit> AddServerViaScanCmd { get; }
        public ReactiveCommand<Unit, Unit> AddServerViaImageCmd { get; }

        //Subscription
        public ReactiveCommand<Unit, Unit> SubSettingCmd { get; }

        public ReactiveCommand<Unit, Unit> SubUpdateCmd { get; }
        public ReactiveCommand<Unit, Unit> SubUpdateViaProxyCmd { get; }
        public ReactiveCommand<Unit, Unit> SubGroupUpdateCmd { get; }
        public ReactiveCommand<Unit, Unit> SubGroupUpdateViaProxyCmd { get; }

        //Setting
        public ReactiveCommand<Unit, Unit> OptionSettingCmd { get; }

        public ReactiveCommand<Unit, Unit> RoutingSettingCmd { get; }
        public ReactiveCommand<Unit, Unit> DNSSettingCmd { get; }
        public ReactiveCommand<Unit, Unit> GlobalHotkeySettingCmd { get; }
        public ReactiveCommand<Unit, Unit> RebootAsAdminCmd { get; }
        public ReactiveCommand<Unit, Unit> ClearServerStatisticsCmd { get; }
        public ReactiveCommand<Unit, Unit> OpenTheFileLocationCmd { get; }

        //Presets
        public ReactiveCommand<Unit, Unit> RegionalPresetDefaultCmd { get; }

        public ReactiveCommand<Unit, Unit> RegionalPresetRussiaCmd { get; }

        public ReactiveCommand<Unit, Unit> RegionalPresetIranCmd { get; }

        public ReactiveCommand<Unit, Unit> ReloadCmd { get; }

        [Reactive]
        public bool BlReloadEnabled { get; set; }

        [Reactive]
        public bool ShowClashUI { get; set; }

        [Reactive]
        public int TabMainSelectedIndex { get; set; }

        #endregion Menu
        private static HttpClient _httpclient = new HttpClient();
        private static string _Basehttpurl = "http://localhost:5249/api/WebApiProxyProvider";
        private bool _hasNextReloadJob = false;

        #region Init

        public MainWindowViewModel(Func<EViewAction, object?, Task<bool>>? updateView)
        {
            _config = AppHandler.Instance.Config;
            _updateView = updateView;
                
            #region WhenAnyValue && ReactiveCommand

            //servers
            AddVmessServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.VMess);
            });
            AddVlessServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.VLESS);
            });
            AddShadowsocksServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.Shadowsocks);
            });
            AddSocksServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.SOCKS);
            });
            AddHttpServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.HTTP);
            });
            AddTrojanServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.Trojan);
            });
            AddHysteria2ServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.Hysteria2);
            });
            AddTuicServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.TUIC);
            });
            AddWireguardServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.WireGuard);
            });
            AddCustomServerCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerAsync(true, EConfigType.Custom);
            });
            AddServerViaClipboardCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                ////await AddServerViaRestApiAsync();
                await AddServerViaClipboardAsync(null);
            });
            AddServerViaScanCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerViaScanAsync();
            });
            AddServerViaImageCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerViaImageAsync();
            });

            //Subscription
            SubSettingCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await SubSettingAsync();
            });

            SubUpdateCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await UpdateSubscriptionProcess("", false);
            });
            SubUpdateViaProxyCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await UpdateSubscriptionProcess("", true);
            });
            SubGroupUpdateCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await UpdateSubscriptionProcess(_config.SubIndexId, false);
            });
            SubGroupUpdateViaProxyCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await UpdateSubscriptionProcess(_config.SubIndexId, true);
            });

            //Setting
            OptionSettingCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await OptionSettingAsync();
            });
            RoutingSettingCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await RoutingSettingAsync();
            });
            DNSSettingCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await DNSSettingAsync();
            });
            GlobalHotkeySettingCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                if (await _updateView?.Invoke(EViewAction.GlobalHotkeySettingWindow, null) == true)
                {
                    NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
                }
            });
            RebootAsAdminCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await RebootAsAdmin();
            });
            ClearServerStatisticsCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ClearServerStatistics();
            });
            OpenTheFileLocationCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenTheFileLocation();
            });

            ReloadCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await Reload();
            });

            RegionalPresetDefaultCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ApplyRegionalPreset(EPresetType.Default);
            });

            RegionalPresetRussiaCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ApplyRegionalPreset(EPresetType.Russia);
            });

            RegionalPresetIranCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await ApplyRegionalPreset(EPresetType.Iran);
            });

            #endregion WhenAnyValue && ReactiveCommand

            _ = Init();
        }

        private async Task Init()
        {
            _config.UiItem.ShowInTaskbar = true;

            await ConfigHandler.InitBuiltinRouting(_config);
            await ConfigHandler.InitBuiltinDNS(_config);
            await ProfileExHandler.Instance.Init();
            await CoreHandler.Instance.Init(_config, UpdateHandler);
            TaskHandler.Instance.RegUpdateTask(_config, UpdateTaskHandler);

            if (_config.GuiItem.EnableStatistics || _config.GuiItem.DisplayRealTimeSpeed)
            {
                await StatisticsHandler.Instance.Init(_config, UpdateStatisticsHandler);
            }

            BlReloadEnabled = true;
            await Reload();
            await AutoHideStartup();
            Task.Factory.StartNew(AddServerViaRestApiAsync, TaskCreationOptions.LongRunning);

            //Task.Factory.StartNew(checkTimertoDelete, TaskCreationOptions.LongRunning);
            Locator.Current.GetService<StatusBarViewModel>()?.RefreshRoutingsMenu();
        }

        #endregion Init

        #region Actions

        private void UpdateHandler(bool notify, string msg)
        {
            NoticeHandler.Instance.SendMessage(msg);
            if (notify)
            {
                NoticeHandler.Instance.Enqueue(msg);
            }
        }

        private void UpdateTaskHandler(bool success, string msg)
        {
            NoticeHandler.Instance.SendMessageEx(msg);
            if (success)
            {
                var indexIdOld = _config.IndexId;
                RefreshServers();
                if (indexIdOld != _config.IndexId)
                {
                    _ = Reload();
                }
                if (_config.UiItem.EnableAutoAdjustMainLvColWidth)
                {
                    _updateView?.Invoke(EViewAction.AdjustMainLvColWidth, null);
                }
            }
        }

        private void UpdateStatisticsHandler(ServerSpeedItem update)
        {
            if (!_config.UiItem.ShowInTaskbar)
            {
                return;
            }
            _updateView?.Invoke(EViewAction.DispatcherStatistics, update);
        }

        public void SetStatisticsResult(ServerSpeedItem update)
        {
            if (_config.GuiItem.DisplayRealTimeSpeed)
            {
                Locator.Current.GetService<StatusBarViewModel>()?.UpdateStatistics(update);
            }
            if (_config.GuiItem.EnableStatistics && (update.ProxyUp + update.ProxyDown) > 0 && DateTime.Now.Second % 9 == 0)
            {
                Locator.Current.GetService<ProfilesViewModel>()?.UpdateStatistics(update);
            }
        }

        public async Task MyAppExitAsync(bool blWindowsShutDown)
        {
            try
            {
                Logging.SaveLog("MyAppExitAsync Begin");
                MessageBus.Current.SendMessage("", EMsgCommand.AppExit.ToString());

                await ConfigHandler.SaveConfig(_config);
                await SysProxyHandler.UpdateSysProxy(_config, true);
                await ProfileExHandler.Instance.SaveTo();
                await StatisticsHandler.Instance.SaveTo();
                StatisticsHandler.Instance.Close();
                await CoreHandler.Instance.CoreStop();

                Logging.SaveLog("MyAppExitAsync End");
            }
            catch { }
            finally
            {
                if (!blWindowsShutDown)
                {
                    _updateView?.Invoke(EViewAction.Shutdown, false);
                }
            }
        }

        public async Task UpgradeApp(string arg)
        {
            if (!Utils.UpgradeAppExists(out var upgradeFileName))
            {
                NoticeHandler.Instance.SendMessageAndEnqueue(ResUI.UpgradeAppNotExistTip);
                Logging.SaveLog("UpgradeApp does not exist");
                return;
            }

            var id = ProcUtils.ProcessStart(upgradeFileName, arg, Utils.StartupPath());
            if (id > 0)
            {
                await MyAppExitAsync(false);
            }
        }

        public void ShowHideWindow(bool? blShow)
        {
            _updateView?.Invoke(EViewAction.ShowHideWindow, blShow);
        }

        public void Shutdown(bool byUser)
        {
            _updateView?.Invoke(EViewAction.Shutdown, byUser);
        }

        #endregion Actions

        #region Servers && Groups

        private void RefreshServers()
        {
            MessageBus.Current.SendMessage("", EMsgCommand.RefreshProfiles.ToString());
        }
        /// <summary>
        /// Using Locator to run Refreshb subscriptions at profileviewmodel
        /// </summary>
        private void RefreshSubscriptions()
        {
            Locator.Current.GetService<ProfilesViewModel>()?.RefreshSubscriptions();
        }
        private async Task<bool> RefreshSubscriptionsAndRemoveDuplicate()
        {
            var profVM = Locator.Current.GetService<ProfilesViewModel>();
            //Locator.Current.GetService<ProfilesViewModel>()?.RefreshSubscriptions();
            if (await profVM?.isDuplicateServer())
            {
                profVM?.RemoveDuplicateServer();
                profVM?.RefreshSubscriptions();
                return true;
            }
            else
            {
                profVM?.RefreshSubscriptions();
                return false;
            }
        }

        #endregion Servers && Groups

        #region Add Servers

        public async Task AddServerAsync(bool blNew, EConfigType eConfigType)
        {
            ProfileItem item = new()
            {
                Subid = _config.SubIndexId,
                ConfigType = eConfigType,
                IsSub = false,
            };

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
                    await Reload();
                }
            }
        }
        /// <summary>
        /// the function connect to rest api and could get new proxy by SSL Validation in Api side 
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetProxyByApiSSL()
        {
            try
            {
                //string httpurl = "http://localhost:5249/";
                string httpurl = ResUI.BaseApiUrl;

                HttpClientHandler handler = new HttpClientHandler();
                //handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                //handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                //handler.ClientCertificates.Add(new X509Certificate2("C:\\MyCert.cer"));

                HttpClient client = new HttpClient(handler);
                client.BaseAddress = new Uri(httpurl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(15);

                //var response = await client.GetAsync("api/WebApiProxyProvider").ConfigureAwait(false);
                var response = await client.GetAsync(ResUI.BaseApiMethod).ConfigureAwait(false);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //var proxies = JsonConvert.DeserializeObject<List<string>>(content).FirstOrDefault();
                    var proxy = JsonConvert.DeserializeObject<string>(content);
                    return proxy;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
        /// <summary>
        /// GetProxy by rest Api 
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetProxyByApiNoneSSL()
        {
            try
            {
                
                //HttpClientHandler handler = new HttpClientHandler();
                //handler.ClientCertificates.Add(clientCert);
                //HttpClient client = new HttpClient(handler);
                var response = await _httpclient.GetAsync(ResUI.BaseApiUrl+ ResUI.BaseApiMethod);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //var proxies = JsonConvert.DeserializeObject<List<string>>(content).FirstOrDefault();
                    var proxy = JsonConvert.DeserializeObject<string>(content);
                    return proxy;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                return null;
            }

        }
        /// <summary>
        /// notify to api by model as  ProxyModelTosendApi with content and speed that show the result of proxy 
        /// </summary>
        /// <param name="prxmodel"></param>
        /// <returns></returns>
        public async Task PostProxyByApiNoneSSL(ProxyModelTosendApi prxmodel)
        {
            try
            {
                //string PostUrl = $"{_Basehttpurl}?ReturnedProxy={strproxy}&speed={ProxySpeed}";
                //var prxmodel = new ProxyModelTosendApi() { Content= "Lorem", Description = "Ipsum" };
                if (prxmodel == null)
                    return;
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(prxmodel);
                var requestContent = new StringContent(jsonData, Encoding.Unicode, "application/json");
                var response = await _httpclient.PostAsync(ResUI.BaseApiUrl+ResUI.BaseApiMethod, requestContent);
                if (response.StatusCode == System.Net.HttpStatusCode.OK | response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    ////var content = await response.Result.Content.ReadAsStringAsync();
                    ////var createdCategory = JsonConvert.DeserializeObject<bool>(content);
                    NoticeHandler.Instance.Enqueue(ResUI.ApiResponseSuccess);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        /// <summary>
        /// Create Task for stating Syncronization via Rest Api.
        /// </summary>
        public async void AddServerViaRestApiAsyncTask()
        {
            Task.Factory.StartNew(AddServerViaRestApiAsync, TaskCreationOptions.LongRunning);
            //Task.Factory.StartNew(checkTimertoDelete, TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// the main Tast to Add new proxy via rest api , the first check Proxy from server by GetProxyByApiSSL() and then Check the timer that if time :00:00:00
        /// checkTimertoDelete() must delete all proxy from that we recieved , then if proxy is not null and via AddBatchServers() check the proxy is exist in database
        /// or no , after the steps the proxy could insert to server and then test the speed by ServerSpeedtest(), by calling PostProxyByApiNoneSSL() we could send proxy string and
        /// also proxy speed to api server
        /// Modified By Nasiri
        /// </summary>
        public async void AddServerViaRestApiAsync()
        {
            while (true)
            {
                
                try
                {
                    Thread.Sleep(6 * 1000);
                    var prxData = GetProxyByApiSSL();
                    if (await checkTimertoDelete())
                    {
                        continue;
                    }
                    if (prxData == null | prxData?.Result == null  )
                    {
                        //await _updateView?.Invoke(EViewAction.AddServerViaClipboard, null);
                        ///Send Notice to UI
                        NoticeHandler.Instance.Enqueue(ResUI.ApiConnectionFailed);
                        continue;
                    }
                    if (prxData?.Result == "")
                    {
                        //await _updateView?.Invoke(EViewAction.AddServerViaClipboard, null);
                        NoticeHandler.Instance.Enqueue(ResUI.ApiproxyNull);
                        
                        continue;
                    }
                   
                    int ret = await ConfigHandler.AddBatchServers(_config, prxData?.Result, _config.SubIndexId, false);
                    if (ret > 0)
                    {
                        RefreshSubscriptions();
                        ////var isduplimported=await RefreshSubscriptionsAndRemoveDuplicate();
                        RefreshServers();
                        var profVm = Locator.Current.GetService<ProfilesViewModel>();
                        var prfItem = FmtHandler.ResolveConfig(prxData?.Result, out string msg);
                        ////ServerSpeedtest(ESpeedActionType.Speedtest,_config.IndexId);
                        /// profVm?.SelectedProfiles =  profVm?.GetProfileItemsExByindex(_config.IndexId).Result;
                        var SelProf =profVm?.GetProfileItemsExByindex(prfItem.Id).Result;
                        profVm?.ServerSpeedtestByselected(ESpeedActionType.Speedtest, SelProf);
                        _updateView?.Invoke(EViewAction.DispatcherSpeedTest, null);
                        Thread.Sleep(10 * 1000);
                        GetSpeedAndPostToApi(SelProf.IndexId, prxData?.Result);
                        NoticeHandler.Instance.Enqueue(string.Format(ResUI.SuccessfullyImportedServerViaClipboard, ret));
                    }
                    else
                    {
                        if (ret == 0)
                            NoticeHandler.Instance.Enqueue(ResUI.menuRemoveDuplicateServer);
                        else
                            NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
                    }
                    
                }
                catch (System.AggregateException ex)
                {
                    NoticeHandler.Instance.Enqueue(ex.Message);
                    Logging.SaveLog("OnApiConnection -> " + ex.Message.ToString());

                }
            }
        }
        /// <summary>
        /// after some delay toupdating the speed of each proxy the method could update the proxy and send it to api
        /// </summary>
        /// <param name="newPrxindex"></param>
        /// <param name="proxystr"></param>
        /// <returns></returns>
        public async Task GetSpeedAndPostToApi(string newPrxindex,string proxystr)
        {
            try
            {
                var profVm = Locator.Current.GetService<ProfilesViewModel>();
                var profitemEX = await profVm.GetProfileItemsEx();
                var specProfid = profitemEX.Where(st => st.IndexId == newPrxindex).FirstOrDefault();
                string speedValue = "0";
                if (specProfid != null)
                    speedValue = (specProfid.SpeedVal == null) ? "0" : specProfid.SpeedVal;
                PostProxyByApiNoneSSL(new ProxyModelTosendApi() { Content = $"{proxystr}", Speed = speedValue });
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        /// <summary>
        /// the function are check the time and at zero time remove all proxies 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> checkTimertoDelete()
        {
            if (DateTime.Now.TimeOfDay == TimeSpan.Zero)
            {
                //var profVm = Locator.Current.GetService<ProfilesViewModel>();
                //await profVm?.RemoveServersAsync();
                _updateView?.Invoke(EViewAction.RemoveAllprofile, null);
                PostProxyByApiNoneSSL(new ProxyModelTosendApi() { Content = $"", Speed ="0" });
                return true;
            }
            else
            {
                //var profVm = Locator.Current.GetService<ProfilesViewModel>();
                //await profVm?.RemoveServersAsync();
                //_updateView?.Invoke(EViewAction.RemoveAllprofile, null);
                //PostProxyByApiNoneSSL(new ProxyModelTosendApi() { Content = $"", Speed = double.Parse("0") });
                return false;
            }
                
        }
        /// <summary>
        /// The function use Profile view to test speed of  each proxy 
        /// </summary>
        /// <param name="actionType"></param>
        /// <returns></returns>
        public async Task ServerSpeedtest(ESpeedActionType actionType)
        {
            var profVm = Locator.Current.GetService<ProfilesViewModel>();
           //// await profVm ?.GetProfileItemsEx(_config.SubIndexId, _serverFilter);
            var lstSelecteds =await profVm?.GetProfileItems(false);
            if (lstSelecteds == null)
            {
                return;
            }
            //ClearTestResult();
            _ = new SpeedtestService(_config, lstSelecteds, actionType, profVm.UpdateSpeedtestHandler);
        }
        /// <summary>
        /// test Proxy speed by index 
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="Prxindex"></param>
        /// <returns></returns>
        public async Task ServerSpeedtest(ESpeedActionType actionType,string Prxindex)
        {
            var profVm = Locator.Current.GetService<ProfilesViewModel>();
            var item = await AppHandler.Instance.GetProfileItem(Prxindex);
            var lstSelecteds = await profVm?.GetProfileItems(false);
            if (lstSelecteds == null)
            {
                return;
            }
            //ClearTestResult();

            _ = new SpeedtestService(_config, lstSelecteds, actionType, profVm.UpdateSpeedtestHandler);
        }
        public async Task AddServerViaClipboardAsync(string? clipboardData)
        {
            if (clipboardData == null)
            {
                await _updateView?.Invoke(EViewAction.AddServerViaClipboard, null);
                return;
            }
            int ret = await ConfigHandler.AddBatchServers(_config, clipboardData, _config.SubIndexId, false);
            if (ret > 0)
            {
                RefreshSubscriptions();
                RefreshServers();
                NoticeHandler.Instance.Enqueue(string.Format(ResUI.SuccessfullyImportedServerViaClipboard, ret));
            }
            else
            {
                NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
            }
        }

        public async Task AddServerViaScanAsync()
        {
            _updateView?.Invoke(EViewAction.ScanScreenTask, null);
            await Task.CompletedTask;
        }

        public async Task ScanScreenResult(byte[]? bytes)
        {
            var result = QRCodeHelper.ParseBarcode(bytes);
            await AddScanResultAsync(result);
        }

        public async Task AddServerViaImageAsync()
        {
            _updateView?.Invoke(EViewAction.ScanImageTask, null);
            await Task.CompletedTask;
        }

        public async Task ScanImageResult(string fileName)
        {
            if (Utils.IsNullOrEmpty(fileName))
            {
                return;
            }

            var result = QRCodeHelper.ParseBarcode(fileName);
            await AddScanResultAsync(result);
        }

        private async Task AddScanResultAsync(string? result)
        {
            if (Utils.IsNullOrEmpty(result))
            {
                NoticeHandler.Instance.Enqueue(ResUI.NoValidQRcodeFound);
            }
            else
            {
                int ret = await ConfigHandler.AddBatchServers(_config, result, _config.SubIndexId, false);
                if (ret > 0)
                {
                    RefreshSubscriptions();
                    RefreshServers();
                    NoticeHandler.Instance.Enqueue(ResUI.SuccessfullyImportedServerViaScan);
                }
                else
                {
                    NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
                }
            }
        }

        #endregion Add Servers

        #region Subscription

        private async Task SubSettingAsync()
        {
            if (await _updateView?.Invoke(EViewAction.SubSettingWindow, null) == true)
            {
                RefreshSubscriptions();
            }
        }

        public async Task UpdateSubscriptionProcess(string subId, bool blProxy)
        {
            await (new UpdateService()).UpdateSubscriptionProcess(_config, subId, blProxy, UpdateTaskHandler);
        }

        #endregion Subscription

        #region Setting

        private async Task OptionSettingAsync()
        {
            var ret = await _updateView?.Invoke(EViewAction.OptionSettingWindow, null);
            if (ret == true)
            {
                Locator.Current.GetService<StatusBarViewModel>()?.InboundDisplayStatus();
                await Reload();
            }
        }

        private async Task RoutingSettingAsync()
        {
            var ret = await _updateView?.Invoke(EViewAction.RoutingSettingWindow, null);
            if (ret == true)
            {
                await ConfigHandler.InitBuiltinRouting(_config);
                Locator.Current.GetService<StatusBarViewModel>()?.RefreshRoutingsMenu();
                await Reload();
            }
        }

        private async Task DNSSettingAsync()
        {
            var ret = await _updateView?.Invoke(EViewAction.DNSSettingWindow, null);
            if (ret == true)
            {
                await Reload();
            }
        }

        public async Task RebootAsAdmin()
        {
            ProcUtils.RebootAsAdmin();
            await MyAppExitAsync(false);
        }

        private async Task ClearServerStatistics()
        {
            await StatisticsHandler.Instance.ClearAllServerStatistics();
            RefreshServers();
        }

        private async Task OpenTheFileLocation()
        {
            var path = Utils.StartupPath();
            if (Utils.IsWindows())
            {
                ProcUtils.ProcessStart(path);
            }
            else if (Utils.IsLinux())
            {
                ProcUtils.ProcessStart("nautilus", path);
            }
            else if (Utils.IsOSX())
            {
                ProcUtils.ProcessStart("open", path);
            }
            await Task.CompletedTask;
        }

        #endregion Setting

        #region core job

        public async Task Reload()
        {
            //If there are unfinished reload job, marked with next job.
            if (!BlReloadEnabled)
            {
                _hasNextReloadJob = true;
                return;
            }

            BlReloadEnabled = false;

            await LoadCore();
            await SysProxyHandler.UpdateSysProxy(_config, false);
            Locator.Current.GetService<StatusBarViewModel>()?.TestServerAvailability();

            _updateView?.Invoke(EViewAction.DispatcherReload, null);

            BlReloadEnabled = true;
            if (_hasNextReloadJob)
            {
                _hasNextReloadJob = false;
                await Reload();
            }
        }

        public void ReloadResult()
        {
            // BlReloadEnabled = true;
            //Locator.Current.GetService<StatusBarViewModel>()?.ChangeSystemProxyAsync(_config.systemProxyItem.sysProxyType, false);
            ShowClashUI = _config.IsRunningCore(ECoreType.sing_box);
            if (ShowClashUI)
            {
                Locator.Current.GetService<ClashProxiesViewModel>()?.ProxiesReload();
            }
            else
            { TabMainSelectedIndex = 0; }
        }

        private async Task LoadCore()
        {
            var node = await ConfigHandler.GetDefaultServer(_config);
            await CoreHandler.Instance.LoadCore(node);
        }

        public async Task CloseCore()
        {
            await ConfigHandler.SaveConfig(_config);
            await CoreHandler.Instance.CoreStop();
        }

        private async Task AutoHideStartup()
        {
            if (_config.UiItem.AutoHideStartup)
            {
                ShowHideWindow(false);
            }
            await Task.CompletedTask;
        }

        #endregion core job

        #region Presets

        public async Task ApplyRegionalPreset(EPresetType type)
        {
            await ConfigHandler.ApplyRegionalPreset(_config, type);
            await ConfigHandler.InitRouting(_config);
            Locator.Current.GetService<StatusBarViewModel>()?.RefreshRoutingsMenu();

            await ConfigHandler.SaveConfig(_config);
            await new UpdateService().UpdateGeoFileAll(_config, UpdateHandler);
            await Reload();
        }

        #endregion Presets
    }
}
