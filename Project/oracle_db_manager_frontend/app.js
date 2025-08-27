const API_BASE = "http://localhost:5500";

//Save Connection
let CONN = loadConnection();

function loadConnection(){
    try{ return JSON.parse(localStorage.getItem('odbm.conn') || '{}');      
    } catch { return{}; }
}

function saveConnection(c){
    CONN = c;
    localStorage.setItem('odbm.conn', JSON.stringify(c));
}
document.addEventListener('DOMContentLoaded', () => {
//Helpers
const qs = (s, el=document) => el.querySelector(s);
const qsa = (s, el=document) => Array.from(el.querySelectorAll(s));
const msg = (t) => {qs('#Message').textContent = t || '';};
const on = (sel, ev, fn) => {const el = qs(sel); if(el) el.addEventListener(ev, fn);};
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

//Connection
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

//Tree
on('#btnLoadTree', 'click', loadTree);
async function loadTree(){
    if(!CONN?.host){
        msg('Please establish a connection first.');
        return;
    }
    qs('#tree').textContent = 'Loading...';
    const res = await fetch(`${API_BASE}/api/Metadata/Tree`, {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({conn:CONN})
    });
    if (!res.ok){
        qs('#tree').textContent = 'Error loading tree.';
        return;
    }
    const tree = await res.json();
    qs('#tree').innerHTML = renderTreeHTML(tree);
}
function renderTreeHTML(tree){
    const owners = Object.keys(tree || {}).sort();
    if(!owners.length) return '<div class="text-gray-500">No hay objetos visibles.</div>';
    return owners.map(owner => {
        const n = tree[owner] || {};
        const list = (title, arr) => `<div class="text-gray-700">${title}</div>
        <ul class="ml-4 list-disc">${(arr||[]).map(x=>`<li>${x}</li>`).join('')}</ul>`;
        return `
        <div class= "mb-3">
        <div class="font-semibold">${owner}</div>
        <div class="ml-3">
            ${list('Tables', n.tables)}
            ${list('Views', n.views)}
            ${list('Procedures', n.procedures)}
            ${list('Functions', n.functions)}
        </div>
        </div>`;
    }).join('');
}

//SQL Runner
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
    const sql = qs('#sql').value;
    const res = await fetch(`${API_BASE}/api/Sql/Execute`, {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({conn:CONN, sql, maxRows: 500})
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

//DDL Modal
on('#btnDDL', 'click', () => {
    resetDDLForm();
    openModal('#ddlModal');
});

//Here we do the alternation of sections depending of the type of DDL
on('#ddlType', 'change', refreshDDLSections);
function refreshDDLSections(){
    const type = qs('#ddlType').value;
    qs('#ddlTableSection').classList.toggle('hidden', type !== 'table');
    qs('#ddlViewSection').classList.toggle('hidden', type !== 'view');
    qs('#ddlProcedureSection').classList.toggle('hidden', type !== 'procedure');
}

//Dinamic Columns
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
    //clean
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
    }
});

});
