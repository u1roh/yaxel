import React, { useState, useEffect } from 'react';
import './App.css';
import Function from './Function'
import * as api from './api'
import * as yaxel from './yaxel'

function Module(props: { name: string }) {
  const [functions, setFunctions] = useState({ tag: 'err', value: 'not initialized' } as api.Result<yaxel.Fun[]>);
  const [breath, setBreath] = useState(-1);
  useEffect(() => {
    console.log("Module: props.name = " + props.name);
    api.fetchModuleFunctions(props.name).then(setFunctions);
    setBreath(-1);
  }, [props.name]);
  useEffect(() => {
    const id = setInterval(async () => {
      const serverBreath = await api.fetchModuleBreathCount(props.name);
      if (serverBreath !== breath) {
        setBreath(serverBreath);
        api.fetchModuleFunctions(props.name).then(setFunctions);
      }
    }, 1000);
    return () => clearInterval(id);
  }, [breath, functions]);
  useEffect(() => {
    console.log(functions);
  }, [functions]);
  return (
    <div className="Module">
      <h2>Functions</h2>
      {functions.tag === 'err' ? <div>ERROR: {JSON.stringify(functions.value)}</div> :
        functions.value.map(item => <Function module={props.name} func={item}></Function>)}
    </div>
  );
}

function NewItem(props: { onNewItem: (name: string) => boolean }) {
  const [isActive, setIsActive] = useState(false);
  const [isError, setIsError] = useState(false);
  const [name, setName] = useState("");
  const clear = () => {
    setIsActive(false);
    setIsError(false);
    setName("");
  };
  console.log("isActive = " + isActive);
  return <div className='NewItem'>
    {isActive ?
      <div className='NewItem-active'>
        <input className={isError ? 'NewItem-input-error' : 'NewItem-input'} onChange={e => setName(e.target.value)} value={name}></input>
        <input className='NewItem-button' type='button' name='add' value='Add'
          onClick={e => props.onNewItem(name) ? clear() : setIsError(true)}></input>
        <input className='NewItem-button' type='button' name='cancel' value='Cancel' onClick={clear}></input>
      </div> :
      <div className='NewItem-nonactive' onClick={() => setIsActive(true)}>+</div>
    }
  </div >;
}

function ModuleList(props: { modules: string[], onSelectedIndexChanged: (index: number) => void, onNewModule: (name: string) => boolean }) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  return (
    <div className="ModuleList">
      <h2>Modules</h2>
      <ul>
        {props.modules.map((item, index) =>
          <li className={index === selectedIndex ? "ItemSelected" : "ItemDeselected"}
            onClick={_ => { setSelectedIndex(index); props.onSelectedIndexChanged(index); console.log("onClick: index = " + index); }}>{item}</li>
        )}
      </ul>
      <NewItem onNewItem={props.onNewModule}></NewItem>
    </div>
  );
}

function CodeEditor(props: { name: string }) {
  const [code, setCode] = useState("");
  useEffect(() => {
    console.log("CodeEditor: props.name = " + props.name);
    api.fetchUserCode(props.name)
      .then(text => setCode(text));
  }, [props.name]);
  const onKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.ctrlKey && e.key === 's') {
      e.preventDefault();
      console.log("Ctrl + S");
      api.updateUserCode(props.name, code);
    }
    else if (e.keyCode === 9) {
      e.preventDefault();
      console.log(1);
      if (e.target instanceof HTMLTextAreaElement) {
        console.log(2);
        const pos = e.target.selectionStart;
        if (pos !== null) {
          console.log(3);
          const txt = e.target.value;
          e.target.value = txt.substr(0, pos) + '\t' + txt.substr(pos, txt.length);
          e.target.setSelectionRange(pos + 1, pos + 1);
        }
      }
    }
  };
  return <div className='CodeEditor'>
    <h2>F# code</h2>
    <ol>
      <li>関数を書く</li>
      <li>Ctrl + S で保存&amp;コンパイル</li>
    </ol>
    <textarea
      className="CodeEditor-textarea"
      value={code}
      onChange={e => setCode(e.target.value)}
      onKeyDown={e => onKeyDown(e)}></textarea>
  </div>;
}

function App() {
  const [modules, setModules] = useState([] as string[]);
  const [breath, setBreath] = useState(-1);
  const [selectedModuleIndex, setSelectedModuleIndex] = useState(0);
  useEffect(() => {
    console.log("App: modules = " + JSON.stringify(modules));
    api.fetchModuleList().then(setModules);
  }, []);
  useEffect(() => {
    const id = setInterval(async () => {
      const serverBreath = await api.fetchBreathCount();
      if (serverBreath !== breath) {
        setBreath(serverBreath);
        api.fetchModuleList().then(setModules);
      }
    }, 1000);
    return () => clearInterval(id);
  }, [breath, modules, selectedModuleIndex]);
  const onNewModule = (name: string) => {
    if (/^\w(\w|\d)*$/.test(name)) {
      api.addNewModule(name).then(() => api.fetchModuleList()).then(setModules);
      return true;
    }
    else { return false; }
  };
  return (
    <div className="App">
      <h1>F# Cloud Run</h1>
      <div className="App-container">
        <div className="App-modules"><ModuleList modules={modules} onSelectedIndexChanged={setSelectedModuleIndex} onNewModule={onNewModule} /></div>
        <div className="App-functions"><Module name={modules[selectedModuleIndex]} /></div>
        <div className="App-editor"><CodeEditor name={modules[selectedModuleIndex]} /></div>
      </div>
    </div>
  );
}

export default App;
