import React, { useState, useEffect } from 'react';
import './App.css';
import Function from './Function'
import * as api from './api'
import * as yaxel from './yaxel'

function Module(props: { name: string }) {
  const [functions, setFunctions] = useState([] as api.Result<yaxel.Fun>[]);
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
  return (
    <div className="Module">
      <h1>Functions</h1>
      {functions.map(item =>
        item.tag === 'ok' ? <Function module={props.name} func={item.value}></Function> : <div>{JSON.stringify(item.value)}</div>
      )}
    </div>
  );
}

function ModuleList(props: { modules: string[], onSelectedIndexChanged: (index: number) => void }) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  return (
    <div className="ModuleList">
      <h1>Modules</h1>
      <ul>
        {props.modules.map((item, index) =>
          <li className={index === selectedIndex ? "ItemSelected" : "ItemDeselected"}
            onClick={_ => { setSelectedIndex(index); props.onSelectedIndexChanged(index); console.log("onClick: index = " + index); }}>{item}</li>
        )}
      </ul>
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
  };
  return <textarea
    className="CodeEditor"
    value={code}
    onChange={e => setCode(e.target.value)}
    onKeyDown={e => onKeyDown(e)}></textarea>
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
  return (
    <div className="App">
      <div className="App-modules"><ModuleList modules={modules} onSelectedIndexChanged={setSelectedModuleIndex} /></div>
      <div className="App-fuctions"><Module name={modules[selectedModuleIndex]} /></div>
      <div className="App-editor"><CodeEditor name={modules[selectedModuleIndex]} /></div>
    </div>
  );
}

export default App;
