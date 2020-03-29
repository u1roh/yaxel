import React from 'react';
import './App.css';
import Function from './Function'
import * as api from './api'

interface State {
    modules: string[]
}

class ModuleList extends React.Component<{}, State> {
    private breathCount: number = 0;
    constructor(props: {}) {
        super(props);
        this.state = { modules: [] };
    }
    private async fetchModules() {
        const modules = await api.fetchModuleList();
        console.log("ModuleList: modules = " + JSON.stringify(modules));
        this.setState({ modules: modules });
    }
    componentDidMount() {
        setInterval(async () => {
            const breath = await api.fetchBreathCount();
            if (breath !== this.breathCount) {
                console.log("breath = " + breath);
                this.breathCount = breath;
                this.fetchModules();
            }
        }, 1000);
        this.fetchModules();
    }
    render() {
        return (
            <div className="ModuleList">
                <h1>Modules</h1>
                <ul>
                    {this.state.modules.map(item => <li>{item}</li>)}
                </ul>
            </div>
        );
    }
}

export default ModuleList;
