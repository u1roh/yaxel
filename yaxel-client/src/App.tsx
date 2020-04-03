import React, { useState, useEffect } from 'react';
import './App.css';
import Function from './Function'
import ModuleList from './ModuleList'
import * as api from './api'
import * as yaxel from './yaxel'

function Module(props: { name: string }) {
  const [functions, setFunctions] = useState([] as api.Result<yaxel.Fun>[]);
  const [breath, setBreath] = useState(-1);
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

function CodeEditor(props: { name: string }) {
  const [code, setCode] = useState("");
  useEffect(() => {
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
  return (
    <div className="App">
      <div className="App-modules"><ModuleList /></div>
      <div className="App-fuctions"><Module name="Sample" /></div>
      <div className="App-editor"><CodeEditor name="Sample" /></div>
    </div>
  );
}

export default App;
