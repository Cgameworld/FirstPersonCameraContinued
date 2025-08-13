import React, { useState, useEffect } from 'react';
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

const register: ModRegistrar = (moduleRegistry) => {

    const { DescriptionTooltip } = VanillaComponentsResolver.instance;

    // Translation.
    function translate(key: string) {
        const { translate } = useLocalization();
        return translate(key);
    }

    let tooltipDescriptionMainCameraIcon: string | null;
    let tooltipDescriptionFollowCamera: string | null;

    let uiTextEnterFreeCamera: string | null;
    let uiTextFollowRandomCim: string | null;
    let uiTextFollowRandomVehicle: string | null;
    let uiTextFollowRandomTransit: string | null;

    const IsEntered$ = bindValue<boolean>('fpc', 'IsEntered');

    const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');

    const CustomMenuButton = () => {

        const [showButtonDropdown, setShowButtonDropdown] = useState(false);

        const toggleButtonDropdown = () => {
            setShowButtonDropdown(!showButtonDropdown);
        };

        const isEntered = useValue(IsEntered$);

        const showCrosshair = useValue(ShowCrosshair$);

        tooltipDescriptionMainCameraIcon = translate("FirstPersonCameraContinued.TooltipMainCameraIcon");
        tooltipDescriptionFollowCamera = translate("FirstPersonCameraContinued.TooltipFollowCamera");

        uiTextEnterFreeCamera = translate("FirstPersonCameraContinued.EnterFreeCamera");
        uiTextFollowRandomCim = translate("FirstPersonCameraContinued.FollowRandomCim");
        uiTextFollowRandomVehicle = translate("FirstPersonCameraContinued.FollowRandomVehicle");
        uiTextFollowRandomTransit = translate("FirstPersonCameraContinued.FollowRandomTransit");

        const uiTextFollowedVehiclePanel = {
            nameLabel: translate("FirstPersonCameraContinued.NameLabel"),
            speedLabel: translate("FirstPersonCameraContinued.SpeedLabel"),
            vehicleTypeLabel: translate("FirstPersonCameraContinued.VehicleTypeLabel"),
            resourcesLabel: translate("FirstPersonCameraContinued.ResourceLabel"),
            actionLabel: translate("FirstPersonCameraContinued.ActionLabel"),
            passengersLabel: translate("FirstPersonCameraContinued.PassengersLabel")
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

                return entity
            }
            if (currentEntity != entity.index) {
                currentEntity = entity.index
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

                    ReactDOM.render(<DropdownWindow onClose={toggleButtonDropdown} />, dropdownRoot);

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
                <button id="FPC-MainGameButton" className="button_ke4 button_ke4 button_h9N" onClick={() => {
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
                x?.__Type === "Game.UI.InGame.UpkeepSection" as any
            ) && !JSON.stringify(titleSection$.value?.name).includes("Decal");

            if (shouldInject) {
                let existingDiv: HTMLDivElement | null = element.querySelector('div.fpc-injected-div');
                if (!existingDiv) {
                    let div: HTMLDivElement = document.createElement('div');
                    div.className = 'fpc-injected-div';
                    ReactDOM.render(FPVInfoWindowButton(), div);

                    element.appendChild(div);

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

    const DropdownWindow: React.FC<{ onClose: () => void }> = ({ onClose }) => {

        const clickedDropdownItem = (item: string) => {
            onClose();
            engine.trigger("audio.playSound", "select-item", 1);
            trigger("fpc", item);
        };

        //dynamically change width of dropdown window based on locale
        const [dropdownWidth, setDropdownWidth] = useState<string>('220rem');
        const [rightOffset, setRightOffset] = useState<string>('0rem');

        useEffect(() => {
            const texts = [uiTextEnterFreeCamera, uiTextFollowRandomCim, uiTextFollowRandomVehicle];

            const calculateWidth = () => {

                //check if text has zh-HANS, zh-HANT, ko, jp characters
                if (!texts.some(text =>
                    text && /[\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u4e00-\u9faf\u3400-\u4dbf]/.test(text))) {
                    const longestText = Math.max(
                        ...(texts.map(text => text?.length || 0))
                    );
                    const calculatedWidth = longestText * 10 + 15;
                    setDropdownWidth(`${calculatedWidth}rem`);
                }
            };

            const updateRightOffset = () => {
                const button = document.querySelector('#FPC-MainGameButton');
                if (button) {
                    const rect = button.getBoundingClientRect();
                    const offset = window.innerWidth - rect.left;
                    const offsetAdjusted = offset / (window.innerWidth / 1920) - 40;
                    setRightOffset(`${offsetAdjusted}rem`);
                }
            };

            calculateWidth();
            updateRightOffset();
        }, [uiTextEnterFreeCamera, uiTextFollowRandomCim, uiTextFollowRandomVehicle]);

        return (
            <div style={{ width: dropdownWidth, right: rightOffset }} className="fpc-dropdownpanel panel_YqS expanded collapsible advisor-panel_dXi advisor-panel_mrr top-right-panel_A2r">
                <div className="content_XD5 content_AD7 child-opacity-transition_nkS">
                    <div className="scrollable_DXr y_SMM scrollable_wt8">
                        <div className="content_gqa" style={{ padding: '0' }} >
                            <div className="infoview-panel-section_RXJ" style={{ padding: '0' }}>
                                <div className="content_1xS focusable_GEc item-focused_FuT" style={{ padding: '0' }}>
                                    <div className="row_S2v fpc-right-row" onClick={() => clickedDropdownItem("ActivateFPC")}>
                                        <div className="right_k3O row_S2v">{uiTextEnterFreeCamera}</div>
                                    </div>
                                    <div className="row_S2v fpc-right-row" onClick={() => clickedDropdownItem("RandomCimFPC")}>
                                        <div className="right_k3O row_S2v">{uiTextFollowRandomCim}</div>
                                    </div>
                                    <div className="row_S2v fpc-right-row" onClick={() => clickedDropdownItem("RandomVehicleFPC")}>
                                        <div className="right_k3O row_S2v">{uiTextFollowRandomVehicle}</div>
                                    </div>
                                    <div className="row_S2v fpc-right-row" onClick={() => clickedDropdownItem("RandomTransitFPC")}>
                                        <div className="right_k3O row_S2v">{uiTextFollowRandomTransit}</div>
                                    </div>
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