import React from 'react';
import logo from './logo.svg';
import './App.css';
import Function from './Function'

interface State {
  functions: string[]
}

class FuncList extends React.Component<{}, State> {
  constructor(props: {}) {
    super(props);
    this.state = { functions: [] };
  }
  componentDidMount() {
    fetch('function')
      .then(res => res?.text())
      .then(body => this.setState({ functions: JSON.parse(body) }));
  }
  render() {
    return (
      <div className="FuncList">
        <h1>Functions</h1>
        {this.state.functions.map(item => <Function name={item}></Function>)}
      </div>
    );
  }
}

function App() {
  return (
    <div className="App">
      <FuncList />
    </div>
  );
}

export default App;
