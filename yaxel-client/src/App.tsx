import React, { useState, useEffect } from 'react';
import './App.css';
import Function from './Function'
import ModuleList from './ModuleList'
import * as api from './api'

function FuncList() {
  const [functions, setFunctions] = useState([] as string[]);
  let breathCount = -1;
  useEffect(() => {
    const id = setInterval(async () => {
      const breath = await api.fetchBreathCount();
      if (breath !== breathCount) {
        console.log("breath = " + breath);
        breathCount = breath;
        api.fetchFunctionList().then(setFunctions);
      }
    }, 1000);
    return () => clearInterval(id);
  });
  return (
    <div className="FuncList">
      <h1>Functions</h1>
      {functions.map(item => <Function name={item}></Function>)}
    </div>
  );

}

function CodeEditor() {
  const [isInitial, setIsInitial] = useState(true);
  const [code, setCode] = useState("");
  useEffect(() => {
    if (isInitial) {
      setIsInitial(false);
      api.fetchUserCode()
        .then(text => setCode(text));
    }
  });
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
      <div className="App-fuctions"><FuncList /></div>
      <div className="App-editor"><CodeEditor /></div>
    </div>
  );
}

export default App;
