const API_BASE = "http://localhost:5242";

let CONN = loadConnection();
let connStr = '';

// Multiconnections helper present originally (not used elsewhere)
function uid(){
  return 'c_' + Math.random().toString(36).slice(2, 10);
}

function loadConnection(){
  try{ return JSON.parse(localStorage.getItem('odbm.conn') || '{}');  
  } catch { return ''; }
}

function saveConnection(c){
  CONN = c;
  localStorage.setItem('odbm.conn', JSON.stringify(c));
}

function getConnString(){
  if(!CONN?.host) return '';

  if(CONN?.user=='sys') {
    return `User Id=${CONN.user};Password=${CONN.password};Data Source=//${CONN.host}:${CONN.port}/${CONN.service};DBA Privilege=SYSDBA;`;
  }else {
    return `User Id=${CONN.user};Password=${CONN.password};Data Source=//${CONN.host}:${CONN.port}/${CONN.service};`;
  }
}

// Minimal fix: provide newId used by renderTreeHTML
let __id = 0;
function newId(prefix='id'){ __id += 1; return `${prefix}-${__id}`; }


document.addEventListener('DOMContentLoaded', () => {
// Helpers
const qs = (s, el=document) => el.querySelector(s);
const qsa = (s, el=document) => Array.from(el.querySelectorAll(s));
const msg = (t) => {qs('#Message').textContent = t || '';};
const on = (sel, ev, fn) => {const el = qs(sel); if(el) el.addEventListener(ev, fn);};
const picker = document.getElementById('connPick');
function openModal(sel){
  const m=qs(sel);
  m.classList.remove('hidden');
  m.classList.add('flex');
}
function closeModal(sel){
  const m=qs(sel);
  m.classList.add('hidden');
  m.classList.remove('flex');
}

// Connection
function fillConnection(){
  qs('#host').value = CONN.host || 'localhost';
  qs('#port').value = CONN.port ?? '1521';
  qs('#service').value = CONN.service || 'FREEPDB1';
  qs('#user').value = CONN.user || '';
  qs('#password').value = CONN.password || '';
}
on('#btnConnSettings', 'click', () => {
  fillConnection();
  openModal('#connModal');
});
qsa('[data-close]').forEach(btn =>
  btn.addEventListener('click', () => closeModal(btn.getAttribute('data-close')))
);
on('#btnSaveConn', 'click', () => 
{
  const conn = {
    host: qs('#host').value.trim(),
    port: Number(qs('#port').value),
    service: qs('#service').value.trim(),
    user: qs('#user').value.trim(),
    password: qs('#password').value
  };

  if(!conn.host || !conn.port || !conn.service || !conn.user){
    msg('Please fill all the connection fields');
    return;
  }

  qs('#Message').textContent = '';
  saveConnection(conn);
  closeModal('#connModal');
  msg('Connection saved successfully. Load the Trees or execute a query.');
});


// Tree
on('#btnLoadTree', 'click', loadTree);
async function loadTree(){
  if(!CONN?.host){
    msg('Please establish a connection first.');
    return;
  }
  connStr = getConnString();

  qs('#treeMsg').textContent = 'Loading...';
  const res = await fetch(`${API_BASE}/api/MetaData/Tree`, {
    method: 'GET',
    headers: { 'ConnectionString': `${connStr}` }
  });
  if (!res.ok){
    qs('#treeMsg').textContent = 'Error loading tree.';
    return;
  }

  const tree = await res.json();


  qs('#tree').innerHTML =
    '<div class="bg-white rounded-sm p-4 dark:bg-neutral-900" role="tree" aria-orientation="vertical" data-hs-tree-view>' +
      renderTreeHTML(tree) +
    '</div>';

  if (window.HSStaticMethods && window.HSStaticMethods.autoInit) {
    window.HSStaticMethods.autoInit();
  }

  qs('#treeMsg').textContent = 'Database Tree';
}

function renderTreeHTML(tree){
  const iconFolder = `
    <svg class="shrink-0 size-4 text-gray-500 dark:text-neutral-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
      <path d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z"/>
    </svg>`;

  const iconFile = `
    <svg class="shrink-0 size-4 text-gray-500 dark:text-neutral-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
      <path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z"/>
      <path d="M14 2v4a2 2 0 0 0 2 2h4"/>
    </svg>`;

  const toggleBtn = (ariaControls, expanded = false) => `
    <button class="hs-accordion-toggle size-6 flex justify-center items-center hover:bg-gray-100 rounded-md focus:outline-hidden focus:bg-gray-100 disabled:opacity-50 disabled:pointer-events-none dark:hover:bg-neutral-700 dark:focus:bg-neutral-700"
            aria-expanded="${expanded}" aria-controls="${ariaControls}">
      <svg class="size-4 text-gray-800 dark:text-neutral-200" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
        <path d="M5 12h14"></path>
        <path class="hs-accordion-active:hidden block" d="M12 5v14"></path>
      </svg>
    </button>`;

  const leaf = (text, value) => `
    <div class="hs-tree-view-selected:bg-gray-100 dark:hs-tree-view-selected:bg-neutral-700 px-2 rounded-md cursor-pointer"
         role="treeitem"
         data-hs-tree-view-item='{"value":"${value}","isDir":false}'>
      <div class="flex items-center gap-x-3">
        ${iconFile}
        <div class="grow">
          <span class="text-sm text-gray-800 dark:text-neutral-200">${text}</span>
        </div>
      </div>
    </div>`;

  const section = (title, items=[]) => {
    if (!items.length) return '';
    const secId = newId('sec');
    const collapseId = newId('collapse');
    const children = items.map(x => leaf(x, x)).join('');
    return `
      <div class="hs-accordion" role="treeitem" aria-expanded="false" id="${secId}"
           data-hs-tree-view-item='{"value":"${title}","isDir":true}'>
        <div class="hs-accordion-heading py-0.5 rounded-md flex items-center gap-x-0.5 w-full hs-tree-view-selected:bg-gray-100 dark:hs-tree-view-selected:bg-neutral-700">
          ${toggleBtn(collapseId, false)}
          <div class="grow hs-tree-view-selected:bg-gray-100 dark:hs-tree-view-selected:bg-neutral-700 px-1.5 rounded-md cursor-pointer">
            <div class="flex items-center gap-x-3">
              ${iconFolder}
              <div class="grow"><span class="text-sm text-gray-800 dark:text-neutral-200">${title}</span></div>
            </div>
          </div>
        </div>
        <div id="${collapseId}" class="hs-accordion-content hidden w-full overflow-hidden transition-[height] duration-300" role="group" aria-labelledby="${secId}">
          <div class="ms-3 ps-3 relative before:absolute before:top-0 before:start-0 before:w-0.5 before:-ms-px before:h-full before:bg-gray-100 dark:before:bg-neutral-700">
            ${children}
          </div>
        </div>
      </div>`;
  };

  const ownerBlock = (owner, n) => {
    const accId = newId('owner');
    const colId = newId('collapse');
    return `
      <div class="hs-accordion" role="treeitem" aria-expanded="false" id="${accId}"
           data-hs-tree-view-item='{"value":"${owner}","isDir":true}'>
        <div class="hs-accordion-heading py-0.5 rounded-md flex items-center gap-x-0.5 w-full hs-tree-view-selected:bg-gray-100 dark:hs-tree-view-selected:bg-neutral-700">
          ${toggleBtn(colId, false)}
          <div class="grow hs-tree-view-selected:bg-gray-100 dark:hs-tree-view-selected:bg-neutral-700 px-1.5 rounded-md cursor-pointer">
            <div class="flex items-center gap-x-3">
              ${iconFolder}
              <div class="grow"><span class="text-sm text-gray-800 dark:text-neutral-200">${owner}</span></div>
            </div>
          </div>
        </div>
        <div id="${colId}" class="hs-accordion-content hidden w-full overflow-hidden transition-[height] duration-300" role="group" aria-labelledby="${accId}">
          <div class="ps-7 relative before:absolute before:top-0 before:start-3 before:w-0.5 before:-ms-px before:h-full before:bg-gray-100 dark:before:bg-neutral-700">
            ${section('Tables', n.tables || [])}
            ${section('Views', n.views || [])}
            ${section('Procedures', n.procedures || [])}
            ${section('Functions', n.functions || [])}
          </div>
        </div>
      </div>`;
  };

  // Minimal fix: define owners locally from tree
  const owners = Object.keys(tree || {});
  const ownersHtml = owners.map(owner => ownerBlock(owner, tree[owner] || {})).join('');

  return `
    <div id="tree-view" class="bg-white rounded-sm p-4 dark:bg-neutral-900"
         role="tree" aria-orientation="vertical" data-hs-tree-view>
      ${ownersHtml}
    </div>`;
}

// SQL Runner
on('#btnRun', 'click', executeSql);
on('#btnClear', 'click', () => {
  qs('#sql').value = '';
  qs('#resultWrap').innerHTML = '';
  msg('');
});

async function executeSql(){
  if(!CONN?.host){
    msg('Please establish a connection first.');
    return;
  }
  const connStr = getConnString();

  const sql = qs('#sql').value;
  const res = await fetch(`${API_BASE}/api/Sql/execute`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({connectionString: `${connStr}`, sql, maxRows: 100})
  });
  const data = await res.json().catch(() => ({}));
  renderResult(data);
  msg(res.ok ? 'Executed' : 'Error executing SQL.');
}

function renderResult(result){
  const wrap = qs('#resultWrap');
  if(result?.columns && result?.rows){
    const head = `<tr>${result.columns.map(c=>`<th class="px-2 py-1 border-b bg-gray-50 text-left">${c}</th>`).join('')}</tr>`;
    const rows = result.rows.map(r=>`<tr>${r.map(c=>`<td class="px-2 py-1 border-b align-top">${c??''}</td>`).join('')}</tr>`).join('');
    wrap.innerHTML = `<table class="w-full text-sm">${head}${rows}</table>`;
  }else{
    wrap.innerHTML = `<div class="text-gray-700">Rows affected: ${result?.rowsAffected ?? 0}. ${result?.Message || ''}</div>`;
  }
}

// DDL Modal
on('#btnDDL', 'click', () => {
  resetDDLForm();
  openModal('#ddlModal');
});

// Here we do the alternation of sections depending of the type of DDL
on('#ddlType', 'change', refreshDDLSections);
function refreshDDLSections(){
  const type = qs('#ddlType').value;
  qs('#ddlTableSection').classList.toggle('hidden', type !== 'table');
  qs('#ddlViewSection').classList.toggle('hidden', type !== 'view');
  qs('#ddlProcedureSection').classList.toggle('hidden', type !== 'procedure');
}

// Dinamic Columns
on('#btnAddCol', 'click', addColumnRow);
function addColumnRow(){
  const wrap = qs('#ddlCols');
  const row = document.createElement('div');
  row.className = 'grid grid-cols-12 gap-2 items-center';
  row.innerHTML = `
      <input placeholder="Name" class="col-span-3 border rounded p-2" data-role="name" value="ID"/>
      <input placeholder="Data Type" class="col-span-3 border rounded p-2" data-role="datatype" value="NUMBER"/>
      <input type="number" placeholder="Length" class="col-span-2 border rounded p-2" data-role="length"/>
      <label class="col-span-2 text-sm"><input type="checkbox" data-role="nullable" />Nullable</label>
      <label class="col-span-1 text-sm"><input type="checkbox" data-role="isPrimaryKey" />PK</label>
      <button class="col-span-1 px-2 py-1 rounded border" data-role="remove">X</button>`;
  wrap.appendChild(row);
  row.querySelector('[data-role="remove"]').addEventListener('click', () => row.remove());
}

function resetDDLForm() {
  qs('#ddlType').value = 'table';
  qs('#ddlSchema').value = 'APP_USER';
  qs('#ddlSelect').value = '';
  qs('#ddlName').value = '';
  qs('#ddlPkName').value = '';
  qs('#ddlSource').value = '';
  // clean
  qs('#ddlCols').innerHTML = '';
  addColumnRow();
  refreshDDLSections();
}

on('#btnDDLCreate', 'click', async () => {
  const type = qs('#ddlType').value;
  if(!CONN?.host) {msg('Please establish a connection first.'); return;}

  if(type === 'table' || type === 'TABLE') {
    const schema = qs('#ddlSchema').value.trim();
    const table = qs('#ddlName').value.trim();
    const pkName = qs('#ddlPkName').value.trim();
    const cols = Array.from(qsa('#ddlCols > div')).map(row => ({
      name: row.querySelector('[data-role="name"]').value.trim(),
      datatype: row.querySelector('[data-role="datatype"]').value.trim(),
      length: row.querySelector('[data-role="length"]').value || null,
      nullable: row.querySelector('[data-role="nullable"]').checked,
      isPrimaryKey: row.querySelector('[data-role="isPrimaryKey"]').checked
    }));

    await createTable(schema, table, pkName, cols);
  }

  if (type === "view" || type === "VIEW")
  {
    const schema = qs('#ddlSchema').value.trim();
    const view = qs('#ddlName').value.trim();
    const selectSql = qs('#ddlSelect').value;

    await createView(schema, view, selectSql);
  }

  if(type === "procedure" || type === "PROCEDURE")
  {
    const schema = qs('#ddlSchema').value.trim();
    const procedure = qs('#ddlName').value.trim();
    const source = qs('#ddlSource').value;

    await createProcedure(schema, procedure, source);
  }
});

async function createTable(schema, table, pkName, cols){
  if(!CONN?.host){
    msg('Please establish a connection first.');
    return;
  }
  const connStr = getConnString();

  const res = await fetch(`${API_BASE}/api/Ddl/create-table`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      connectionString: `${connStr}`, 
      schema: `${schema}`, 
      tableName: `${table}`,
      columns: cols.map(col => ({
        name: `${col.name}`,
        dataType: `${col.datatype}`,
        length: col.length,
        nullable: col.nullable,
        isPrimaryKey: col.isPrimaryKey
      })),
      primaryKeyName: `${pkName}`
    })
  });
  const data = await res.json().catch(() => ({}));
  renderResult(data);
  msg(res.ok ? 'Executed' : 'Error executing SQL.');

  closeModal('#ddlModal');
  msg(res.ok ? 'Table created successfully. Load the Trees or execute a query.' : 'Error creating table.');
}

async function createView(schema, view, selectSql){
  if(!CONN?.host){
    msg('Please establish a connection first.');
    return;
  }
  const connStr = getConnString();

  const res = await fetch(`${API_BASE}/api/Ddl/create-view`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      connectionString: `${connStr}`,
      schema: `${schema}`,
      viewName: `${view}`,
      selectSql: `${selectSql}`
    })
  });

  const data = await res.json().catch(() => ({}));
  renderResult(data);
  msg(res.ok ? 'Executed' : 'Error executing SQL.');
  closeModal('#ddlModal');
  msg(res.ok ? 'View created successfully' : 'Error creating view');
}

async function createProcedure(schema, procedure, source)
{
  if(!CONN?.host){
    msg('Please establish a connection first.');
    return;
  }
  const connStr = getConnString();

  const res = await fetch(`${API_BASE}/api/Ddl/create-procedure`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      connectionString: `${connStr}`,
      schema: `${schema}`,
      procedureName: `${procedure}`,
      source: `${source}`
    })
  });

  const data = await res.json().catch(() => ({}));
  renderResult(data);
  msg(res.ok ? 'Executed' : 'Error executing SQL.');
  closeModal('#ddlModal');
  msg(res.ok ? 'Procedure created successfully' : 'Error creating procedure');
}

});
