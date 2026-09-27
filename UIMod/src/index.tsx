import React, { useState, useEffect, useRef } from 'react';
import { ModRegistrar } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Entity, selectedInfo } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import { VanillaComponentsResolver } from "../types/internal";
import ReactDOM from 'react-dom';
import 'style/DropdownWindow.scss';
import 'style/Crosshair.scss';
import 'style/EntityInfo.scss';
import engine from 'cohtml/cohtml';
import FollowedVehicleInfoPanel from './panels/FollowedVehicleInfoPanel';
import StopStripPanel from './panels/StopStripPanel';
import ChangelogWindow from './changelogWindow';
import RandomFollowWindow from './randomFollowWindow';
import 'style/RandomFollowWindow.scss';

const register: ModRegistrar = (moduleRegistry) => {

    const { DescriptionTooltip } = VanillaComponentsResolver.instance;

    // Translation.
    function translate(key: string) {
        const { translate } = useLocalization();
        return translate(key);
    }

    const playHoverSound = () => {
        engine.trigger("audio.playSound", "hover-item", 1);
    };

    let tooltipDescriptionMainCameraIcon: string | null;
    let tooltipDescriptionFollowCamera: string | null;

    let uiTextEnterFreeCamera: string | null;
    let uiTextFollowRandom: string | null;
    let uiTextFollowRandomCitizen: string | null;
    let uiTextFollowRandomVehicle: string | null;
    let uiTextFollowRandomTransit: string | null;
    let uiTextFollowRandomCustom: string | null;

    const IsEntered$ = bindValue<boolean>('fpc', 'IsEntered');

    const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');

    const ShowChangelog$ = bindValue<boolean>('fpc', 'ShowChangelog');

    const NumKeyEvent$ = bindValue<number>('fpc', 'NumKeyEvent');

    const CustomMenuButton = () => {

        const [showButtonDropdown, setShowButtonDropdown] = useState(false);
        const [showRandomFollowWindow, setShowRandomFollowWindow] = useState(false);

        const toggleButtonDropdown = () => {
            setShowButtonDropdown(!showButtonDropdown);
        };

        const isEntered = useValue(IsEntered$);

        const showCrosshair = useValue(ShowCrosshair$);

        const showChangelog = useValue(ShowChangelog$);

        tooltipDescriptionMainCameraIcon = translate("FirstPersonCameraContinued.TooltipMainCameraIcon");
        tooltipDescriptionFollowCamera = translate("FirstPersonCameraContinued.TooltipFollowCamera");

        uiTextEnterFreeCamera = translate("FirstPersonCameraContinued.EnterFreeCamera");
        uiTextFollowRandom = translate("FirstPersonCameraContinued.FollowRandom");
        uiTextFollowRandomCitizen = translate("FirstPersonCameraContinued.FollowRandom.Citizen");
        uiTextFollowRandomVehicle = translate("FirstPersonCameraContinued.FollowRandom.Vehicle");
        uiTextFollowRandomTransit = translate("FirstPersonCameraContinued.FollowRandom.Transit");
        uiTextFollowRandomCustom = translate("FirstPersonCameraContinued.FollowRandom.Custom");

        const uiTextFollowedVehiclePanel = {
            nameLabel: translate("FirstPersonCameraContinued.NameLabel"),
            speedLabel: translate("FirstPersonCameraContinued.SpeedLabel"),
            altitudeLabel: translate("FirstPersonCameraContinued.AltitudeLabel"),
            vehicleTypeLabel: translate("FirstPersonCameraContinued.VehicleTypeLabel"),
            resourcesLabel: translate("FirstPersonCameraContinued.ResourceLabel"),
            actionLabel: translate("FirstPersonCameraContinued.ActionLabel"),
            passengersLabel: translate("FirstPersonCameraContinued.PassengersLabel")
        };

        const uiTextRandomFollowWindow = {
            title: uiTextFollowRandom,
            selectAll: translate("FirstPersonCameraContinued.RandomFollow.SelectAll"),
            deselectAll: translate("FirstPersonCameraContinued.RandomFollow.DeselectAll"),
            start: translate("FirstPersonCameraContinued.RandomFollow.Start"),
        };

        var selectedEntity: Entity;
        if (selectedInfo && selectedInfo.selectedEntity$) {
            selectedInfo.selectedEntity$.subscribe(SelectedEntityChanged);
        }
        function SelectedEntityChanged(newEntity: Entity) {
            selectedEntity = newEntity;
            trigger("fpc", "SelectedEntity", newEntity);
        }

        //keep track of current selected entity - from plop the growables
        const selectedEntity$ = selectedInfo.selectedEntity$;
        let currentEntity: any = null;

        selectedEntity$.subscribe((entity) => {
            if (!entity.index) {
                currentEntity = null;
                cleanupInjectedButton();
                return entity
            }
            if (currentEntity != entity.index) {
                currentEntity = entity.index
                cleanupInjectedButton();
            }
            observeAndAppend();
            return entity;
        })

        useEffect(() => {
            if (showButtonDropdown) {
                //const mainGameButton = document.querySelector('#FPC-MainGameButton');
                const mainGameButton = document.querySelector('.main-container__E2');
                if (mainGameButton && mainGameButton.parentNode) {
                    const dropdownRoot = document.createElement('div');
                    dropdownRoot.id = 'top-right-layout_sSC';

                    mainGameButton.parentNode.appendChild(dropdownRoot);

                    ReactDOM.render(<DropdownWindow onClose={toggleButtonDropdown} onOpenRandomFollow={() => setShowRandomFollowWindow(true)} />, dropdownRoot);

                    return () => {
                        ReactDOM.unmountComponentAtNode(dropdownRoot);
                        if (dropdownRoot.parentNode) {
                            dropdownRoot.parentNode.removeChild(dropdownRoot);
                        }
                    };
                }
            }
        }, [showButtonDropdown]);

        useEffect(() => {
            if (showChangelog) {
                const parentElement = document.querySelector('.main-container__E2');
                if (parentElement) {
                    const changelogRoot = document.createElement('div');
                    changelogRoot.id = 'fpc-changelog-root';
                    changelogRoot.style.width = '100%';
                    parentElement.appendChild(changelogRoot);
                    ReactDOM.render(<ChangelogWindow />, changelogRoot);
                    return () => {
                        ReactDOM.unmountComponentAtNode(changelogRoot);
                        if (changelogRoot.parentNode) {
                            changelogRoot.parentNode.removeChild(changelogRoot);
                        }
                    };
                }
            }
        }, [showChangelog]);

        useEffect(() => {
            if (showRandomFollowWindow) {
                const parentElement = document.querySelector('.main-container__E2');
                if (parentElement) {
                    const randomFollowRoot = document.createElement('div');
                    randomFollowRoot.id = 'fpc-random-follow-root';
                    randomFollowRoot.style.width = '100%';
                    parentElement.appendChild(randomFollowRoot);
                    ReactDOM.render(<RandomFollowWindow onClose={() => setShowRandomFollowWindow(false)} translation={uiTextRandomFollowWindow} />, randomFollowRoot);
                    return () => {
                        ReactDOM.unmountComponentAtNode(randomFollowRoot);
                        if (randomFollowRoot.parentNode) {
                            randomFollowRoot.parentNode.removeChild(randomFollowRoot);
                        }
                    };
                }
            }
        }, [showRandomFollowWindow]);

        useEffect(() => {
            if (isEntered) {
                const injectionPoint = document.body;
                const newDiv = document.createElement('div');
                newDiv.className = 'firstpersoncameracontinued_strippanel';
                injectionPoint.insertBefore(newDiv, injectionPoint.firstChild);

                ReactDOM.render(<StopStripPanel />, newDiv);

                return () => {
                    ReactDOM.unmountComponentAtNode(newDiv);
                    if (newDiv.parentNode) {
                        newDiv.parentNode.removeChild(newDiv);
                    }
                };
            }
        }, [isEntered]);

        useEffect(() => {
            if (isEntered) {
                const injectionPoint = document.body;
                const newDiv = document.createElement('div');
                newDiv.className = 'firstpersoncameracontinued_entityinfo';
                injectionPoint.insertBefore(newDiv, injectionPoint.firstChild);

                ReactDOM.render(<FollowedVehicleInfoPanel translation={uiTextFollowedVehiclePanel} />, newDiv);

                return () => {
                    ReactDOM.unmountComponentAtNode(newDiv);
                    if (newDiv.parentNode) {
                        newDiv.parentNode.removeChild(newDiv);
                    }
                };
            }
        }, [isEntered]);

        useEffect(() => {
            if (showCrosshair) {
                const div = document.querySelector('.game-main-screen_TRK.child-opacity-transition_nkS');

                const crosshairX = document.createElement('div');
                crosshairX.id = "crosshairX-fpc";

                const crosshairY = document.createElement('div');
                crosshairY.id = "crosshairY-fpc";

                div?.appendChild(crosshairX);
                div?.appendChild(crosshairY);
            }
            else {
                document.getElementById('crosshairX-fpc')?.remove();
                document.getElementById('crosshairY-fpc')?.remove();
            }

        }, [showCrosshair]);

        return <div>
            <DescriptionTooltip title="First Person Camera" description={tooltipDescriptionMainCameraIcon}>
                <button id="FPC-MainGameButton" className="button_ke4 button_ke4 button_h9N" onMouseEnter={playHoverSound} onClick={() => {
                    engine.trigger("audio.playSound", "select-item", 1);
                    toggleButtonDropdown();
                }}>
                    <div className="tinted-icon_iKo icon_be5" style={{ backgroundImage: 'url(coui://uil/Standard/VideoCamera.svg)', backgroundPositionX: '1rem', backgroundPositionY: '1rem', backgroundColor: 'rgba(255,255,255,0)', backgroundSize: '35rem 35rem' }}>
                    </div>
                </button>
            </DescriptionTooltip>
        </div>;
    }

    moduleRegistry.append('GameTopRight', CustomMenuButton);

    const middleSections$ = selectedInfo.middleSections$;
    const titleSection$ = selectedInfo.titleSection$;

    let injectedButtonRoot: HTMLDivElement | null = null;

    //fallback if unmount leaves a stuck balloon, remove only the orphaned follow camera tooltip after react has settled
    const clearOrphanedTooltip = (): void => {
        requestAnimationFrame(() => {
            if (!tooltipDescriptionFollowCamera) return;
            const balloons: NodeListOf<Element> = document.querySelectorAll('[class*="balloon"]');
            balloons.forEach((balloon: Element) => {
                if (balloon.textContent?.includes(tooltipDescriptionFollowCamera as string)) {
                    balloon.parentNode?.removeChild(balloon);
                }
            });
        });
    };

    //unmount the injected follow button so its vanilla tooltip clears when the info window closes
    const cleanupInjectedButton = (): void => {
        if (injectedButtonRoot) {
            ReactDOM.unmountComponentAtNode(injectedButtonRoot);
            if (injectedButtonRoot.parentNode) {
                injectedButtonRoot.parentNode.removeChild(injectedButtonRoot);
            }
            injectedButtonRoot = null;
            clearOrphanedTooltip();
        }
    };

    //inject the item into the DOM manually, can't figure out how to put the button in the same row in the official UI system
    const observeAndAppend = (): void => {
        // Clear any existing interval
        if ((window as any).fpcObserverInterval) {
            clearInterval((window as any).fpcObserverInterval);
        }

        //uses polling instead of MutationObserver
        const checkAndInject = () => {
            const element: HTMLElement | null = document.querySelector('.actions-section_X1x');

            if (!element) return;
            const shouldInject = !middleSections$.value.some(x =>
                x?.__Type === "Game.UI.InGame.LevelSection" as any ||
                x?.__Type === "Game.UI.InGame.RoadSection" as any ||
                x?.__Type === "Game.UI.InGame.ResidentsSection" as any ||
                x?.__Type === "Game.UI.InGame.UpkeepSection" as any ||
                x?.__Type === "Game.UI.InGame.VehicleCountSection" as any
            ) && !JSON.stringify(titleSection$.value?.name).includes("Decal");

            if (shouldInject) {
                let existingDiv: HTMLDivElement | null = element.querySelector('div.fpc-injected-div');
                if (!existingDiv) {
                    let div: HTMLDivElement = document.createElement('div');
                    div.className = 'fpc-injected-div';
                    ReactDOM.render(FPVInfoWindowButton(), div);

                    const outOfServiceDiv: HTMLElement | null = element.querySelector('.out-of-service_Kfh');
                    if (outOfServiceDiv) {
                        element.insertBefore(div, outOfServiceDiv);
                    } else {
                        element.appendChild(div);
                    }

                    injectedButtonRoot = div;

                    console.log('New div appended:', div);
                    // Clear interval after successful injection
                    clearInterval((window as any).fpcObserverInterval);
                    delete (window as any).fpcObserverInterval;
                }
            }
        };

        // Check immediately
        checkAndInject();

        // Set up polling as backup
        (window as any).fpcObserverInterval = setInterval(checkAndInject, 100);

        // Clear after reasonable timeout
        setTimeout(() => {
            if ((window as any).fpcObserverInterval) {
                clearInterval((window as any).fpcObserverInterval);
                delete (window as any).fpcObserverInterval;
            }
        }, 5000);
    };

    const FPVInfoWindowButton = () => {
        return (
            <DescriptionTooltip title="First Person Camera" description={tooltipDescriptionFollowCamera}>
                <button style={{ marginLeft: '6rem', marginRight: '8rem' }} className="ok button_Z9O button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_Z9O button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_xGY" onClick={() => trigger("fpc", "EnterFollowFPC")}>
                    <img className="icon_Tdt icon_soN icon_Iwk" src="coui://uil/Colored/VideoCamera.svg"></img>
                </button>
            </DescriptionTooltip>
        );
    }

    interface DropdownMenuItem {
        label: string | null;
        action?: string;
        onCustomClick?: () => void;
        submenu?: DropdownMenuItem[];
    }

    const DropdownWindow: React.FC<{ onClose: () => void; onOpenRandomFollow: () => void }> = ({ onClose, onOpenRandomFollow }) => {

        const clickedDropdownItem = (item: string) => {
            onClose();
            engine.trigger("audio.playSound", "select-item", 1);
            trigger("fpc", item);
        };

        const menuItems: DropdownMenuItem[] = [
            { label: uiTextEnterFreeCamera, action: "ActivateFPC" },
            { label: uiTextFollowRandom, submenu: [
                { label: uiTextFollowRandomCitizen, action: "RandomCimFPC" },
                { label: uiTextFollowRandomVehicle, action: "RandomVehicleFPC" },
                { label: uiTextFollowRandomTransit, action: "RandomTransitFPC" },
                { label: uiTextFollowRandomCustom, onCustomClick: () => { onClose(); engine.trigger("audio.playSound", "select-item", 1); onOpenRandomFollow(); } },
            ]},
        ];

        const handleItemClick = (item: DropdownMenuItem) => {
            if (item.submenu) return;
            if (item.onCustomClick) {
                item.onCustomClick();
            } else if (item.action) {
                clickedDropdownItem(item.action);
            }
        };

        const [submenuDirection, setSubmenuDirection] = useState<string>('fpc-submenu-left');

        const numKeyEvent = useValue(NumKeyEvent$);
        const lastNumKeyEvent = useRef(numKeyEvent);
        const [keyboardSubmenuIndex, setKeyboardSubmenuIndex] = useState<number | null>(null);

        useEffect(() => {
            trigger("fpc", "IsDropdownVisible", true);
            return () => {
                trigger("fpc", "IsDropdownVisible", false);
            };
        }, []);

        //num key quick select, digit is lowest decimal place of event value, first press picks top level item, next press picks inside opened submenu
        useEffect(() => {
            if (numKeyEvent === lastNumKeyEvent.current) return;
            lastNumKeyEvent.current = numKeyEvent;
            const digit = numKeyEvent % 10;

            if (keyboardSubmenuIndex === null) {
                const item = menuItems[digit - 1];
                if (!item) return;
                if (item.submenu) {
                    engine.trigger("audio.playSound", "select-item", 1);
                    setKeyboardSubmenuIndex(digit - 1);
                } else {
                    handleItemClick(item);
                }
            } else {
                const subItem = menuItems[keyboardSubmenuIndex]?.submenu?.[digit - 1];
                if (subItem) {
                    handleItemClick(subItem);
                }
            }
        }, [numKeyEvent]);

        //dynamically change width of dropdown window based on locale
        const [dropdownWidth, setDropdownWidth] = useState<string>('220rem');
        const [submenuWidth, setSubmenuWidth] = useState<string>('120rem');
        const [rightOffset, setRightOffset] = useState<string>('0rem');

        useEffect(() => {
            const texts = menuItems.map(item => item.label);
            const submenuTexts = menuItems.flatMap(item => item.submenu ? item.submenu.map(sub => sub.label) : []);

            const isAsian = (t: string | null) => t && /[\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u4e00-\u9faf\u3400-\u4dbf\uac00-\ud7a3\u1100-\u11ff\u3130-\u318f]/.test(t);

            if (!texts.some(isAsian)) {
                const longestText = Math.max(
                    ...(texts.map(text => text?.length || 0))
                );
                const calculatedWidth = longestText * 10 + 20;
                setDropdownWidth(`${calculatedWidth}rem`);
            }

            let computedSubmenuWidth = 120;
            if (!submenuTexts.some(isAsian)) {
                const longestSubmenuText = Math.max(
                    ...(submenuTexts.map(text => text?.length || 0))
                );
                computedSubmenuWidth = longestSubmenuText * 10 + 40;
                setSubmenuWidth(`${computedSubmenuWidth}rem`);
            }

            const button = document.querySelector('#FPC-MainGameButton');
            if (button) {
                const rect = button.getBoundingClientRect();
                const offset = window.innerWidth - rect.left;
                const offsetAdjusted = offset / (window.innerWidth / 1920) - 40;
                setRightOffset(`${offsetAdjusted}rem`);
                setSubmenuDirection(offsetAdjusted > computedSubmenuWidth ? 'fpc-submenu-right' : 'fpc-submenu-left');
            }
        }, [uiTextEnterFreeCamera, uiTextFollowRandom]);

        return (
            <div style={{ width: dropdownWidth, right: rightOffset }} className="fpc-dropdownpanel panel_YqS expanded collapsible advisor-panel_dXi advisor-panel_mrr top-right-panel_A2r">
                <div className="fpc-panel-blur"></div>
                <div className="content_XD5 content_AD7 child-opacity-transition_nkS">
                    <div className="scrollable_DXr y_SMM scrollable_wt8">
                        <div className="content_gqa" style={{ padding: '0' }} >
                            <div className="infoview-panel-section_RXJ" style={{ padding: '0' }}>
                                <div className="content_1xS focusable_GEc item-focused_FuT" style={{ padding: '0' }}>
                                    {menuItems.map((item, index) => (
                                        <div key={index} className={`fpc-submenu-wrapper${keyboardSubmenuIndex === index ? ' fpc-submenu-keyboard-open' : ''}`}>
                                            <div className={`row_S2v fpc-right-row ${item.submenu ? 'fpc-has-submenu' : ''}`}
                                                onMouseEnter={playHoverSound}
                                                onClick={() => handleItemClick(item)}>
                                                <div className="right_k3O row_S2v">{item.label}</div>
                                                {item.submenu && <span className="fpc-submenu-arrow">&#9654;</span>}
                                            </div>
                                            {item.submenu && (
                                                <div style={{ width: submenuWidth }} className={`fpc-submenu-container ${submenuDirection} panel_YqS expanded collapsible advisor-panel_dXi advisor-panel_mrr top-right-panel_A2r`}>
                                                    <div className="content_XD5 content_AD7 child-opacity-transition_nkS">
                                                        <div className="content_1xS focusable_GEc item-focused_FuT" style={{ padding: '0' }}>
                                                            {item.submenu.map((subItem, subIndex) => (
                                                                <div key={subIndex} className="row_S2v fpc-right-row"
                                                                    onMouseEnter={playHoverSound}
                                                                    onClick={() => handleItemClick(subItem)}>
                                                                    <div className="right_k3O row_S2v">{subItem.label}</div>
                                                                </div>
                                                            ))}
                                                        </div>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    ))}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    };
}

export default register;