const {app,BrowserWindow,Menu}=require('electron');
app.whenReady().then(()=>{Menu.setApplicationMenu(null);const w=new BrowserWindow({width:1180,height:820,minWidth:850,minHeight:650,title:'Goalball Sonoro',backgroundColor:'#08121c',webPreferences:{nodeIntegration:false,contextIsolation:true,sandbox:true}});w.webContents.setWindowOpenHandler(()=>({action:'deny'}));w.webContents.on('will-navigate',e=>e.preventDefault());w.loadFile('index.html');});
app.on('window-all-closed',()=>app.quit());
