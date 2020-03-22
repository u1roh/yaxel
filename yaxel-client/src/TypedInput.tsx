import React from 'react';
import * as yaxel from './yaxel'
import './TypedInput.css';

interface UnionInputProps {
    union: yaxel.UnionType;
    value: any;
    onChange: (value: any) => void;
}

class UnionInput extends React.Component<UnionInputProps> {
    private selectedIndex = 0;
    private setSelectedIndex(i: number) {
        this.props.onChange({
            name: this.props.union.cases[i].name,
            value: yaxel.defaultValueOf(this.props.union.cases[i].type)
        });
    }
    render() {
        return (<div>
            <select onChange={(e) => this.setSelectedIndex(e.target.selectedIndex)} value={this.props.value["name"]}>
                {this.props.union.cases.map(item =>
                    <option value={item.name}>{item.name}</option>)}
            </select>
            {
                (() => {
                    const item = this.props.union.cases.find(item => item.name == this.props.value["name"]);
                    return item !== undefined ?
                        <TypedInput name='' type={item.type} value={this.props.value["value"]}
                            onChange={x => {
                                const obj = this.props.value;
                                obj.value = x;
                                this.props.onChange(obj);
                            }} />
                        : <span></span>;
                })()
            }
        </div>);
    }
}

interface TypedInputProps {
    name: string;
    type: yaxel.Type;
    value: any;
    onChange: (value: any) => void;
}

class TypedInput extends React.Component<TypedInputProps> {
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
            case null:
                return <span></span>;
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
                            const record = this.props.value;
                            record[name] = value;
                            this.onChange(record);
                        };
                        return (<div>
                            <div>{this.caption()}{this.props.type.name}</div>
                            {this.props.type.fields.map(item =>
                                <div><TypedInput name={item.name} type={item.type} value={this.props.value[item.name]} onChange={(x) => onChange(item.name, x)} /></div>
                            )}
                        </div>);
                    case 'union':
                        return <div>{this.caption()}<UnionInput union={this.props.type} value={this.props.value} onChange={x => this.onChange(x)} /></div>;
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
