import React from 'react';
import * as yaxel from './yaxel'
import './TypedInput.css';

interface UnionInputProps {
    union: yaxel.UnionType;
    onChange: (value: any) => void;
}

interface UnionInputState {
    selectedCaseName: string
    selectedCaseType: yaxel.Type | null
}

class UnionInput extends React.Component<UnionInputProps, UnionInputState> {
    constructor(props: UnionInputProps) {
        super(props);
        this.state = {
            selectedCaseName: props.union.cases[0].name,
            selectedCaseType: props.union.cases[0].type
        }
    }
    private setSelectedIndex(i: number) {
        this.setState({
            selectedCaseName: this.props.union.cases[i].name,
            selectedCaseType: this.props.union.cases[i].type
        });
        if (this.props.union.cases[i].type == null) {
            this.props.onChange(this.props.union.cases[i].name);
        }
    }
    render() {
        return (<div>
            <select onChange={(e) => this.setSelectedIndex(e.target.selectedIndex)}>
                {this.props.union.cases.map(item =>
                    <option value={item.name}>{item.name}</option>)}
            </select>
            {this.state.selectedCaseType == null ?
                <span></span> :
                <TypedInput name='' type={this.state.selectedCaseType}
                    onChange={x => {
                        const obj: any = {};
                        obj[this.state.selectedCaseName] = x;
                        this.props.onChange(obj);
                    }} />}
        </div>);
    }
}

interface TypedInputProps {
    name: string;
    type: yaxel.Type;
    onChange: (value: any) => void;
}

class TypedInput extends React.Component<TypedInputProps, { value: any }> {
    constructor(props: TypedInputProps) {
        super(props);
        this.state = { value: null };
    }
    private caption() {
        return this.props.name.length === 0 ? "" : this.props.name + " = ";
    }
    private onChange(x: any) {
        console.log("TypedInput#onChange: " + x);
        this.props.onChange(x);
        this.setState({ value: x });
    }
    private renderInternal() {
        switch (this.props.type) {
            case "int":
            case "float":
            case "string":
                return <span>{this.caption()}
                    <input onChange={(e) => this.onChange(e.target.value)}></input>
                </span>;
            case "bool":
                return <span>
                    <input type='checkbox' onChange={(e) => this.onChange(e.target.checked)}></input>
                    <label>{this.props.name}</label>
                </span>;
            default:
                if (Array.isArray(this.props.type)) {
                    return JSON.stringify(this.props.type);
                }
                switch (this.props.type.tag) {
                    case 'record':
                        const onChange = (name: string, value: any) => {
                            console.log("record / onChange: name = " + name + ", value = " + JSON.stringify(value));
                            const record = this.state.value ? this.state.value : {};
                            record[name] = value;
                            this.onChange(record);
                        };
                        return (<div>
                            <div>{this.caption()}{this.props.type.name}</div>
                            {this.props.type.fields.map(item =>
                                <div><TypedInput name={item.name} type={item.type} onChange={(x) => onChange(item.name, x)} /></div>
                            )}
                        </div>);
                    case 'union':
                        return <div>{this.caption()}<UnionInput union={this.props.type} onChange={x => this.onChange(x)} /></div>;
                    default:
                        return JSON.stringify(this.props.type);
                }
        }
    }
    render() {
        return <div className="TypedInput">{this.renderInternal()}</div>;
    }
}

export default TypedInput;
