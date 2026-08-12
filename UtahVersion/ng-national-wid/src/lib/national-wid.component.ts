import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { NationalWidApiService } from './national-wid-api.service';

export type TabId = 'status' | 'laus' | 'ces' | 'industry' | 'wages' | 'projections' | 'licensing' | 'cpi' | 'nonCoreLookups' | 'nonCoreViews';

export interface TableState {
  loading: boolean;
  rows: any[];
  total: number;
  page: number;
  pageSize: number;
  error: string;
  downloading: boolean;
  downloadError: string;
}

type SortDirection = 'asc' | 'desc';

interface SortControl {
  field: string;
  direction: SortDirection;
}

interface SortFieldOption {
  value: string;
  label: string;
}

interface TableMetadataData {
  areas?: Array<{ stFips: string; areaType: string; areaTypeVersion: string; area: string }>;
  years?: string[];
  periods?: Array<{ periodType: string; period: string }>;
  minPeriod?: { periodYear?: string | null; periodType?: string | null; period?: string | null } | null;
  maxPeriod?: { periodYear?: string | null; periodType?: string | null; period?: string | null } | null;
  projectedYears?: string[];
}

interface MetadataState {
  loading: boolean;
  error: string;
  data: TableMetadataData | null;
}

function freshState(): TableState {
  return { loading: false, rows: [], total: 0, page: 1, pageSize: 25, error: '', downloading: false, downloadError: '' };
}

function freshMetadataState(): MetadataState {
  return { loading: false, error: '', data: null };
}

/**
 * Self-contained National WID data explorer component.
 *
 * Embed this in any Angular application that has access to a National WID API endpoint.
 * Configure the API URL and authentication via `NATIONAL_WID_CONFIG`:
 *
 * ```typescript
 * // app.module.ts
 * providers: [
 *   provideNationalWid({
 *     apiBaseUrl: 'https://api.wid.example.gov/',
 *     getToken: () => myAuthService.getAccessToken()
 *   })
 * ]
 * ```
 *
 * Then in your template:
 * ```html
 * <nwid-national-wid (navigateBack)="router.navigate(['/'])"></nwid-national-wid>
 * ```
 *
 * @output navigateBack — emitted when the user clicks the "Back" navigation button.
 *   The host application is responsible for routing; this component does not import
 *   `@angular/router` and does not navigate itself.
 */
@Component({
  selector: 'nwid-national-wid',
  templateUrl: './national-wid.component.html',
  styleUrls: ['./national-wid.component.css'],
  standalone: false,
})
export class NationalWidComponent implements OnInit {
  /**
   * Emitted when the user activates the "back" navigation control.
   * The host application is responsible for responding (e.g., `router.navigate(['/'])`).
   */
  @Output() navigateBack = new EventEmitter<void>();

  private readonly controlsElementByTab: Record<TabId, string> = {
    status: 'status-controls',
    laus: 'laus-controls',
    ces: 'ces-controls',
    industry: 'industry-controls',
    wages: 'wages-controls',
    projections: 'projections-controls',
    licensing: 'licensing-controls',
    cpi: 'cpi-controls',
    nonCoreLookups: 'non-core-lookups-controls',
    nonCoreViews: 'non-core-views-controls'
  };

  private readonly lookupEndpointByDataset: Record<string, string> = {
    geographies: 'lookups/geographies',
    areatypes: 'lookups/areaTypes',
    statefips: 'lookups/stateFips',
    periodtypes: 'lookups/periodTypes',
    periodyears: 'lookups/periodYears',
    periods: 'lookups/periods',
    industrycodes: 'lookups/industryCodes',
    occupationcodes: 'lookups/occupationCodes',
    cescodes: 'lookups/cesCodes',
    ownerships: 'lookups/ownerships',
    wagesources: 'lookups/wageSources',
    wageratetypes: 'lookups/wageRateTypes',
    benchmark: 'lookups/benchmarks',
    growthcodes: 'lookups/growthCodes',
    inddirectories: 'lookups/ind-directories',
    occdirectories: 'lookups/occ-directories',
    matrixxind: 'projections/matrixXInd',
    matrixxocc: 'projections/matrixXOcc',
    licenseauthorities: 'licensing/authorities',
    licensehistory: 'licensing/history',
    licensexocc: 'licensing/occupationCrosswalks',
    cpiseries: 'cpi/metadata',
    cpiitems: 'cpi/items',
    cpiareas: 'cpi/areas'
  };

  private readonly viewEndpointByDataset: Record<string, string> = {
    ceswithgeography: 'views/cesWithGeography',
    laborforcewithgeography: 'views/laborForceWithGeography',
    industrywithgeography: 'views/industryWithGeography',
    wagewithdescriptions: 'views/wagesWithDescriptions',
    projectionwithtitles: 'views/projectionsWithTitles',
    licensingbyoccupation: 'views/licensingByOccupation'
  };

  activeTab: TabId = 'status';

  // ---- Status ----
  statusLoading = true;
  statusData: any = null;
  statusError = '';
  expandedStatusDataset = '';
  statusHistoryLoading = false;
  statusHistoryError = '';
  statusHistoryData: any = null;

  // ---- Per-table state ----
  lausState = freshState();
  cesState  = freshState();
  indState  = freshState();
  wageState = freshState();
  projState = freshState();
  licensingState = freshState();
  cpiState = freshState();
  lookupState = freshState();
  viewState = freshState();

  lausMetadata = freshMetadataState();
  cesMetadata = freshMetadataState();
  indMetadata = freshMetadataState();
  wageMetadata = freshMetadataState();
  projMetadata = freshMetadataState();
  licensingParams = {
    endpoint: 'licensing/licenses',
    stFips: '',
    licAuthID: '',
    licenseID: '',
    licenseType: ''
  };
  licensingSort: SortControl = { field: '', direction: 'asc' };
  readonly licensingSortFields: SortFieldOption[] = [
    { value: 'stFips', label: 'State FIPS' },
    { value: 'licAuthID', label: 'Licensing Authority' },
    { value: 'licenseID', label: 'License ID' },
    { value: 'licenseType', label: 'License Type' }
  ];
  readonly licensingEndpoints: Array<{ value: string; label: string }> = [
    { value: 'licensing/licenses', label: 'Licensing Records' },
    { value: 'licensing/authorities', label: 'Licensing Authorities' },
    { value: 'licensing/history', label: 'Licensing History' },
    { value: 'licensing/occupationCrosswalks', label: 'Licensing Occupation Crosswalks' }
  ];
  cpiParams = {
    endpoint: 'cpi',
    seriesId: '',
    year: '',
    period: '',
    areaCode: '',
    itemCode: '',
    seasonalCode: ''
  };
  cpiSort: SortControl = { field: '', direction: 'desc' };
  readonly cpiSortFields: SortFieldOption[] = [
    { value: 'seriesId', label: 'Series ID' },
    { value: 'year', label: 'Year' },
    { value: 'period', label: 'Period' },
    { value: 'value', label: 'Value' }
  ];
  readonly cpiEndpoints: Array<{ value: string; label: string }> = [
    { value: 'cpi', label: 'CPI Observations' },
    { value: 'cpi/metadata', label: 'CPI Series Metadata' },
    { value: 'cpi/items', label: 'CPI Items' },
    { value: 'cpi/areas', label: 'CPI Areas' }
  ];

  // ---- LAUS params ----
  lausParams = { stFips: '', areaType: '', area: '', periodYear: '', periodType: '', adjusted: '' };
  lausSort: SortControl = { field: '', direction: 'desc' };
  readonly lausSortFields: SortFieldOption[] = [
    { value: 'periodYear', label: 'Period Year' },
    { value: 'period', label: 'Period' },
    { value: 'laborForce', label: 'Labor Force' },
    { value: 'employed', label: 'Employed' },
    { value: 'unemployed', label: 'Unemployed' },
    { value: 'unempRate', label: 'Unemployment Rate' }
  ];

  // ---- CES params ----
  cesParams = {
    stFips: '', areaType: '', area: '',
    seriesCode: 'CES0000000001', seriesCodeType: '', periodYear: '', adjusted: ''
  };
  cesSort: SortControl = { field: '', direction: 'desc' };
  readonly cesSortFields: SortFieldOption[] = [
    { value: 'periodYear', label: 'Period Year' },
    { value: 'period', label: 'Period' },
    { value: 'seriesCode', label: 'Series Code' },
    { value: 'empCES', label: 'Employment (CES)' },
    { value: 'earningsPerWeek', label: 'Earnings Per Week' },
    { value: 'earningsPerHour', label: 'Earnings Per Hour' }
  ];

  // ---- Industry params ----
  indParams = {
    stFips: '', areaType: '', area: '',
    periodYear: '', periodType: '', ownership: '', codeType: '', indCode: ''
  };
  indSort: SortControl = { field: '', direction: 'desc' };
  readonly indSortFields: SortFieldOption[] = [
    { value: 'periodYear', label: 'Period Year' },
    { value: 'period', label: 'Period' },
    { value: 'avgMonthlyEmp', label: 'Avg Monthly Emp' },
    { value: 'totalWages', label: 'Total Wages' },
    { value: 'weeklyWage', label: 'Weekly Wage' },
    { value: 'empCount', label: 'Establishments' }
  ];

  // ---- Wages params ----
  wageParams = {
    stFips: '', areaType: '', area: '', periodYear: '',
    occCodeType: '', occCode: '', indCodeType: '', indCode: ''
  };
  wageSort: SortControl = { field: '', direction: 'desc' };
  readonly wageSortFields: SortFieldOption[] = [
    { value: 'periodYear', label: 'Period Year' },
    { value: 'occCode', label: 'Occupation Code' },
    { value: 'empCount', label: 'Employment' },
    { value: 'medianWage', label: 'Median Wage' },
    { value: 'meanWage', label: 'Mean Wage' },
    { value: 'meanHourly', label: 'Mean Hourly' }
  ];

  // ---- Projections params ----
  projParams = {
    stFips: '', areaType: '', area: '', projectionsPeriod: '',
    occCodeType: '', occCode: '', indCodeType: '', indCode: ''
  };
  viewParams = {
    endpoint: 'views/cesWithGeography',
    stFips: '',
    areaType: '',
    area: '',
    periodYear: '',
    projectionsPeriod: '',
    seriesCode: '',
    occCode: '',
    indCode: ''
  };
  lookupParams = {
    endpoint: 'lookups/geographies',
    stFips: '',
    areaType: '',
    area: '',
    periodType: '',
    periodYear: '',
    codeType: '',
    code: '',
    seriesCode: '',
    ownership: '',
    growthCode: ''
  };
  lookupSort: SortControl = { field: '', direction: 'asc' };
  readonly nonCoreLookupEndpoints: Array<{ value: string; label: string }> = [
    { value: 'lookups/geographies', label: 'Geographies' },
    { value: 'lookups/areaTypes', label: 'Area Types' },
    { value: 'lookups/stateFips', label: 'State FIPS' },
    { value: 'lookups/periodTypes', label: 'Period Types' },
    { value: 'lookups/periodYears', label: 'Period Years' },
    { value: 'lookups/periods', label: 'Periods' },
    { value: 'lookups/industryCodes', label: 'Industry Codes' },
    { value: 'lookups/occupationCodes', label: 'Occupation Codes' },
    { value: 'lookups/cesCodes', label: 'CES Codes' },
    { value: 'lookups/ownerships', label: 'Ownerships' },
    { value: 'lookups/wageSources', label: 'Wage Sources' },
    { value: 'lookups/wageRateTypes', label: 'Wage Rate Types' },
    { value: 'lookups/benchmarks', label: 'Benchmarks' },
    { value: 'lookups/growthCodes', label: 'Growth Codes' },
    { value: 'lookups/ind-directories', label: 'Projection Industry Directories' },
    { value: 'lookups/occ-directories', label: 'Projection Occupation Directories' },
    { value: 'projections/matrixXInd', label: 'Projection Matrix Industry Crosswalks' },
    { value: 'projections/matrixXOcc', label: 'Projection Matrix Occupation Crosswalks' },
    { value: 'licensing/authorities', label: 'Licensing Authorities' },
    { value: 'licensing/licenses', label: 'Licensing Records' },
    { value: 'licensing/history', label: 'Licensing History' },
    { value: 'licensing/occupationCrosswalks', label: 'Licensing Occupation Crosswalks' }
  ];
  readonly nonCoreLookupSortFields: SortFieldOption[] = [
    { value: 'stFips', label: 'State FIPS' },
    { value: 'areaType', label: 'Area Type' },
    { value: 'periodType', label: 'Period Type' },
    { value: 'periodYear', label: 'Period Year' },
    { value: 'codeType', label: 'Code Type' },
    { value: 'code', label: 'Code' },
    { value: 'seriesCode', label: 'Series Code' },
    { value: 'ownership', label: 'Ownership' },
    { value: 'growthCode', label: 'Growth Code' }
  ];
  viewSort: SortControl = { field: '', direction: 'desc' };
  readonly nonCoreViewEndpoints: Array<{ value: string; label: string }> = [
    { value: 'views/cesWithGeography', label: 'CES with Geography' },
    { value: 'views/laborForceWithGeography', label: 'Labor Force with Geography' },
    { value: 'views/industryWithGeography', label: 'Industry with Geography' },
    { value: 'views/wagesWithDescriptions', label: 'Wages with Descriptions' },
    { value: 'views/projectionsWithTitles', label: 'Projections with Titles' },
    { value: 'views/licensingByOccupation', label: 'Licensing by Occupation' }
  ];
  readonly nonCoreViewSortFields: SortFieldOption[] = [
    { value: 'periodYear', label: 'Period Year' },
    { value: 'period', label: 'Period' },
    { value: 'projectionsPeriod', label: 'Projection Period' },
    { value: 'seriesCode', label: 'Series Code' },
    { value: 'occCode', label: 'Occupation Code' },
    { value: 'indCode', label: 'Industry Code' },
    { value: 'areaName', label: 'Area Name' }
  ];
  projSort: SortControl = { field: '', direction: 'desc' };
  readonly projSortFields: SortFieldOption[] = [
    { value: 'projectionsPeriod', label: 'Projection Period' },
    { value: 'occCode', label: 'Occupation Code' },
    { value: 'indCode', label: 'Industry Code' },
    { value: 'projectedEmp', label: 'Projected Employment' },
    { value: 'change', label: 'Change' },
    { value: 'pctChange', label: 'Percent Change' },
    { value: 'openings', label: 'Openings' }
  ];

  readonly sortDirections: Array<{ value: SortDirection; label: string }> = [
    { value: 'asc', label: 'Ascending' },
    { value: 'desc', label: 'Descending' }
  ];

  constructor(private apiService: NationalWidApiService) {}

  ngOnInit(): void {
    this.loadStatus();
  }

  selectTab(tab: TabId): void {
    this.activeTab = tab;
  }

  /** Emits the {@link navigateBack} output so the host application can handle navigation. */
  routeBack(): void {
    this.navigateBack.emit();
  }

  // ---- Status ----
  loadStatus(): void {
    this.statusLoading = true;
    this.statusError = '';
    this.apiService.get<any>('status').then(result => {
      this.statusLoading = false;
      if (result.success) {
        this.statusData = result.data;
        this.expandedStatusDataset = '';
        this.statusHistoryData = null;
        this.statusHistoryError = '';
      } else {
        this.statusError = result.error ?? 'Failed to load API status.';
      }
    });
  }

  get coreDatasets(): any[] {
    return (this.statusData?.datasets ?? []).filter((x: any) => x.tableClass === 'core');
  }

  get nonCoreDatasets(): any[] {
    return (this.statusData?.datasets ?? []).filter((x: any) => x.tableClass === 'non-core');
  }

  toggleStatusHistory(ds: any): void {
    const dataSet = ds?.dataSet ? String(ds.dataSet) : '';
    if (!dataSet) return;

    if (this.expandedStatusDataset === dataSet) {
      this.expandedStatusDataset = '';
      return;
    }

    this.expandedStatusDataset = dataSet;
    this.loadStatusHistory(dataSet);
  }

  private loadStatusHistory(dataSet: string): void {
    this.statusHistoryLoading = true;
    this.statusHistoryError = '';
    this.statusHistoryData = null;

    const qs = `report=history&dataSet=${encodeURIComponent(dataSet)}&page=1&pageSize=50`;
    this.apiService.get<any>(`status?${qs}`).then(result => {
      this.statusHistoryLoading = false;
      if (result.success) {
        this.statusHistoryData = result.data;
      } else {
        this.statusHistoryError = result.error ?? 'Failed to load status history.';
      }
    });
  }

  isStatusHistoryOpen(ds: any): boolean {
    return this.expandedStatusDataset === String(ds?.dataSet ?? '');
  }

  shortHash(value?: string | null): string {
    if (!value) return '—';
    if (value.length <= 16) return value;
    return `${value.slice(0, 8)}...${value.slice(-8)}`;
  }

  goToDatasetControls(ds: any): void {
    const dataSet = ds?.dataSet ? String(ds.dataSet) : '';
    const key = this.datasetKey(dataSet);

    if (!dataSet) {
      return;
    }

    if (key === 'license') { this.licensingParams.endpoint = 'licensing/licenses'; this.selectTabAndScroll('licensing'); this.queryLicensing(); return; }
    if (key === 'cpiseries') { this.cpiParams.endpoint = 'cpi/metadata'; this.selectTabAndScroll('cpi'); this.queryCpi(); return; }
    if (key === 'cpiitems') { this.cpiParams.endpoint = 'cpi/items'; this.selectTabAndScroll('cpi'); this.queryCpi(); return; }
    if (key === 'cpiareas') { this.cpiParams.endpoint = 'cpi/areas'; this.selectTabAndScroll('cpi'); this.queryCpi(); return; }
    if (key === 'cpi') { this.cpiParams.endpoint = 'cpi'; this.selectTabAndScroll('cpi'); this.queryCpi(); return; }

    if (key === 'laborforce') { this.selectTabAndScroll('laus'); return; }
    if (key === 'ces')        { this.selectTabAndScroll('ces');  return; }
    if (key === 'industry')   { this.selectTabAndScroll('industry'); return; }
    if (key === 'iowage')     { this.selectTabAndScroll('wages'); return; }
    if (key === 'projectionsmatrix') { this.selectTabAndScroll('projections'); return; }

    const lookupEndpoint = this.lookupEndpointByDataset[key];
    if (lookupEndpoint) {
      this.lookupParams.endpoint = lookupEndpoint;
      this.selectTabAndScroll('nonCoreLookups');
      return;
    }

    const viewEndpoint = this.viewEndpointByDataset[key];
    if (viewEndpoint) {
      this.viewParams.endpoint = viewEndpoint;
      this.selectTabAndScroll('nonCoreViews');
      return;
    }

    this.openStatusHistory(dataSet);
  }

  private datasetKey(value: unknown): string {
    if (!value) return '';
    return String(value).replace(/[^a-z0-9]/gi, '').toLowerCase();
  }

  private selectTabAndScroll(tab: TabId): void {
    this.activeTab = tab;
    const elementId = this.controlsElementByTab[tab];
    setTimeout(() => {
      const el = document.getElementById(elementId);
      if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 0);
  }

  private openStatusHistory(dataSet: string): void {
    this.activeTab = 'status';
    this.expandedStatusDataset = dataSet;
    this.loadStatusHistory(dataSet);
  }

  // ---- Generic paginated query helpers ----
  private buildPath(endpoint: string, params: Record<string, string>, page: number, pageSize: number): string {
    const merged = { ...params, page: String(page), pageSize: String(pageSize) };
    const qs = Object.entries(merged)
      .filter(([, v]) => v && v.trim() !== '')
      .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v.trim())}`)
      .join('&');
    return qs ? `${endpoint}?${qs}` : endpoint;
  }

  private async runQuery(state: TableState, endpoint: string, params: Record<string, string>): Promise<void> {
    state.loading = true;
    state.error = '';
    state.rows = [];
    const path = this.buildPath(endpoint, params, state.page, state.pageSize);
    const result = await this.apiService.get<any>(path);
    state.loading = false;
    if (result.success && result.data) {
      state.rows = result.data.data ?? [];
      state.total = result.data.meta?.total ?? 0;
    } else {
      state.error = result.error ?? 'Query failed.';
    }
  }

  private withSort(params: Record<string, string>, sort: SortControl): Record<string, string> {
    if (!sort.field) return { ...params };
    return { ...params, sort: `${sort.field}:${sort.direction}` };
  }

  private buildMetadataPath(endpoint: string, params: Record<string, string>): string {
    const merged = { ...params, metadataFields: 'areas,years,periods,minPeriod,maxPeriod,projectedYears' };
    const qs = Object.entries(merged)
      .filter(([k, v]) => k !== 'sort' && v && v.trim() !== '')
      .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v.trim())}`)
      .join('&');
    return qs ? `${endpoint}/metadata?${qs}` : `${endpoint}/metadata`;
  }

  private async loadMetadata(state: MetadataState, endpoint: string, params: Record<string, string>): Promise<void> {
    state.loading = true;
    state.error = '';
    const path = this.buildMetadataPath(endpoint, params);
    const result = await this.apiService.get<any>(path);
    state.loading = false;
    if (result.success && result.data?.data) {
      state.data = result.data.data as TableMetadataData;
      return;
    }
    state.data = null;
    state.error = result.error ?? 'Metadata query failed.';
  }

  // ---- Table query methods ----
  queryLaus(page = 1): void {
    this.lausState.page = page;
    this.runQuery(this.lausState, 'labor-force', this.withSort(this.lausParams as any, this.lausSort));
    this.loadMetadata(this.lausMetadata, 'labor-force', this.lausParams as any);
  }

  queryCes(page = 1): void {
    this.cesState.page = page;
    this.runQuery(this.cesState, 'ces', this.withSort(this.cesParams as any, this.cesSort));
    this.loadMetadata(this.cesMetadata, 'ces', this.cesParams as any);
  }

  queryIndustry(page = 1): void {
    this.indState.page = page;
    this.runQuery(this.indState, 'industry', this.withSort(this.indParams as any, this.indSort));
    this.loadMetadata(this.indMetadata, 'industry', this.indParams as any);
  }

  queryWages(page = 1): void {
    this.wageState.page = page;
    this.runQuery(this.wageState, 'wages', this.withSort(this.wageParams as any, this.wageSort));
    this.loadMetadata(this.wageMetadata, 'wages', this.wageParams as any);
  }

  queryProjections(page = 1): void {
    this.projState.page = page;
    this.runQuery(this.projState, 'projections', this.withSort(this.projParams as any, this.projSort));
    this.loadMetadata(this.projMetadata, 'projections', this.projParams as any);
  }

  queryLicensing(page = 1): void {
    this.licensingState.page = page;
    const { endpoint, ...filters } = this.licensingParams;
    this.runQuery(this.licensingState, endpoint, this.withSort(filters as any, this.licensingSort));
  }

  queryCpi(page = 1): void {
    this.cpiState.page = page;
    const { endpoint, ...filters } = this.cpiParams;
    this.runQuery(this.cpiState, endpoint, this.withSort(filters as any, this.cpiSort));
  }

  refreshLausMetadata(): void      { this.loadMetadata(this.lausMetadata, 'labor-force', this.lausParams as any); }
  refreshCesMetadata(): void       { this.loadMetadata(this.cesMetadata, 'ces', this.cesParams as any); }
  refreshIndustryMetadata(): void  { this.loadMetadata(this.indMetadata, 'industry', this.indParams as any); }
  refreshWagesMetadata(): void     { this.loadMetadata(this.wageMetadata, 'wages', this.wageParams as any); }
  refreshProjectionsMetadata(): void { this.loadMetadata(this.projMetadata, 'projections', this.projParams as any); }

  queryNonCoreLookups(page = 1): void {
    this.lookupState.page = page;
    const endpoint = this.lookupParams.endpoint;
    const { endpoint: _, ...filters } = this.lookupParams;
    this.runQuery(this.lookupState, endpoint, this.withSort(filters as any, this.lookupSort));
  }

  queryNonCoreViews(page = 1): void {
    this.viewState.page = page;
    const endpoint = this.viewParams.endpoint;
    const { endpoint: _, ...filters } = this.viewParams;
    this.runQuery(this.viewState, endpoint, this.withSort(filters as any, this.viewSort));
  }

  // ---- Pagination ----
  totalPages(state: TableState): number {
    return Math.max(1, Math.ceil(state.total / state.pageSize));
  }

  canPrev(state: TableState): boolean { return state.page > 1; }
  canNext(state: TableState): boolean { return state.page < this.totalPages(state); }

  goPrev(state: TableState, fn: (p: number) => void): void {
    if (this.canPrev(state)) fn(state.page - 1);
  }

  goNext(state: TableState, fn: (p: number) => void): void {
    if (this.canNext(state)) fn(state.page + 1);
  }

  // ---- Downloads ----
  private async doDownload(state: TableState, endpoint: string, params: Record<string, string>, format: string): Promise<void> {
    state.downloading = true;
    state.downloadError = '';
    const filtered = Object.fromEntries(Object.entries(params).filter(([, v]) => v && v.trim() !== ''));
    const qs = Object.entries({ ...filtered, format })
      .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
      .join('&');
    const path = `${endpoint}?${qs}`;
    const extMap: Record<string, string> = {
      csv: 'csv', tsv: 'tsv', psv: 'txt', jsonfile: 'json', xlsx: 'xlsx'
    };
    const ext = extMap[format] ?? format;
    const fileName = `${endpoint}-${new Date().toISOString().slice(0, 10)}.${ext}`;
    const result = await this.apiService.download(path, fileName);
    state.downloading = false;
    if (!result.success) state.downloadError = result.error ?? 'Download failed.';
  }

  downloadLaus(format: string): void      { this.doDownload(this.lausState, 'labor-force', this.withSort(this.lausParams as any, this.lausSort), format); }
  downloadCes(format: string): void       { this.doDownload(this.cesState, 'ces', this.withSort(this.cesParams as any, this.cesSort), format); }
  downloadIndustry(format: string): void  { this.doDownload(this.indState, 'industry', this.withSort(this.indParams as any, this.indSort), format); }
  downloadWages(format: string): void     { this.doDownload(this.wageState, 'wages', this.withSort(this.wageParams as any, this.wageSort), format); }
  downloadProjections(format: string): void { this.doDownload(this.projState, 'projections', this.withSort(this.projParams as any, this.projSort), format); }

  downloadLicensing(format: string): void {
    const { endpoint, ...filters } = this.licensingParams;
    this.doDownload(this.licensingState, endpoint, this.withSort(filters as any, this.licensingSort), format);
  }

  downloadCpi(format: string): void {
    const { endpoint, ...filters } = this.cpiParams;
    this.doDownload(this.cpiState, endpoint, this.withSort(filters as any, this.cpiSort), format);
  }

  downloadNonCoreLookups(format: string): void {
    const endpoint = this.lookupParams.endpoint;
    const { endpoint: _, ...filters } = this.lookupParams;
    this.doDownload(this.lookupState, endpoint, this.withSort(filters as any, this.lookupSort), format);
  }

  downloadNonCoreViews(format: string): void {
    const endpoint = this.viewParams.endpoint;
    const { endpoint: _, ...filters } = this.viewParams;
    this.doDownload(this.viewState, endpoint, this.withSort(filters as any, this.viewSort), format);
  }

  // ---- Utilities ----
  objectKeys(row: any): string[] {
    if (!row) return [];
    return Object.keys(row);
  }

  fNum(v: number | null | undefined): string {
    return v == null ? '—' : v.toLocaleString('en-US');
  }

  fCur(v: number | null | undefined): string {
    if (v == null) return '—';
    return '$' + v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  fPct(v: number | null | undefined): string {
    return v == null ? '—' : v.toFixed(1) + '%';
  }

  fAdj(v: string | null | undefined): string {
    if (v === '1') return 'SA';
    if (v === '0') return 'NSA';
    return v ?? '—';
  }

  fSupp(v: string | null | undefined): string {
    return v === '1' ? '⚑' : '';
  }

  metadataYearRange(metaState: MetadataState): string {
    const years = metaState.data?.years ?? [];
    if (!years.length) return '—';
    return years.length === 1 ? years[0] : `${years[0]} to ${years[years.length - 1]}`;
  }

  metadataPeriodPoint(point?: { periodYear?: string | null; periodType?: string | null; period?: string | null } | null): string {
    if (!point) return '—';
    const value = [point.periodYear, point.periodType, point.period].filter(x => !!x).join('-');
    return value || '—';
  }
}
