import React, { useState, useEffect } from 'react';
import './App.css';
import Function from './Function'
import ModuleList from './ModuleList'
import * as api from './api'
import * as yaxel from './yaxel'

function Module(props: { name: string }) {
  let breathCount = -1;
  const [functions, setFunctions] = useState([] as api.Result<yaxel.Fun>[]);
  useEffect(() => {
    const id = setInterval(async () => {
      const breath = await api.fetchBreathCount();
      if (breath !== breathCount) {
        console.log("breath = " + breath);
        breathCount = breath;
        api.fetchModuleFunctions("Sample").then(setFunctions);
      }
    }, 1000);
    return () => clearInterval(id);
  }, [functions]);
  return (
    <div className="Module">
      <h1>Functions</h1>
      {functions.map(item =>
        item.tag == 'ok' ? <Function func={item.value}></Function> : <div>{JSON.stringify(item.value)}</div>
      )}
    </div>
  );
}

function CodeEditor() {
  const [code, setCode] = useState("");
  useEffect(() => {
    api.fetchUserCode()
      .then(text => setCode(text));
  }, []); // 空の配列 [] を指定しておくと最初の一回だけこれが呼ばれる
  const onKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.ctrlKey && e.key === 's') {
      e.preventDefault();
      console.log("Ctrl + S");
      api.updateUserCode(code);
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
      <div className="App-editor"><CodeEditor /></div>
    </div>
  );
}

export default App;
