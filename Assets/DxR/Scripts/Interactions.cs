﻿using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DxR
{
    public class Interactions : MonoBehaviour
    {
        // Y offset for placement of filter objects.
        float curYOffset = 0;

        Vis targetVis = null;
        // Each data field's filter result. Each list is the same as the number of 
        // mark instances.
        public Dictionary<string, List<bool>> filterResults = null;

        Dictionary<string, List<string>> domains = null;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Init(Vis vis)
        {
            targetVis = vis;

            if(targetVis != null)
            {
                filterResults = new Dictionary<string, List<bool>>();
                domains = new Dictionary<string, List<string>>();
            }
        }

        internal void AddToggleFilter(JSONObject interactionSpecs)
        {
            if(gameObject.transform.Find(interactionSpecs["field"].Value) != null)
            {
                Debug.Log("Will not duplicate existing filter for field " + interactionSpecs["field"].Value);
                return;
            }

            GameObject toggleFilterPrefab = Resources.Load("GUI/ToggleFilter", typeof(GameObject)) as GameObject;
            if (toggleFilterPrefab == null) return;

            GameObject toggleFilterInstance = Instantiate(toggleFilterPrefab, gameObject.transform);

            toggleFilterInstance.transform.Find("ToggleFilterLabel").gameObject.GetComponent<TextMesh>().text =
                "Toggle " + interactionSpecs["field"].Value + ":";

            toggleFilterInstance.name = interactionSpecs["field"];

            HoloToolkit.Unity.Collections.ObjectCollection collection = toggleFilterInstance.GetComponent<HoloToolkit.Unity.Collections.ObjectCollection>();
            if (collection == null) return;

            // Use the provided domain of the data field to create check boxes.
            // For each checkbox, add it to the interactiveset object, and add it to the object
            // collection object and update the layout.
            GameObject checkBoxPrefab = Resources.Load("GUI/CheckBox", typeof(GameObject)) as GameObject;
            if (checkBoxPrefab == null) return;

            HoloToolkit.Examples.InteractiveElements.InteractiveSet checkBoxSet =
                toggleFilterInstance.GetComponent<HoloToolkit.Examples.InteractiveElements.InteractiveSet>();
            if (checkBoxSet == null) return;

            List<string> domain = new List<string>();

            checkBoxSet.SelectedIndices.Clear();
            int i = 0;
            foreach (JSONNode category in interactionSpecs["domain"].AsArray)
            {
                GameObject checkBoxInstance = Instantiate(checkBoxPrefab, toggleFilterInstance.transform);

                Debug.Log("Creating toggle button for " + category.Value);
                checkBoxInstance.transform.Find("CheckBoxOutline/Label").gameObject.GetComponent<TextMesh>().text = category.Value;

                domain.Add(category.Value);

                checkBoxSet.Interactives.Add(checkBoxInstance.GetComponent<HoloToolkit.Examples.InteractiveElements.InteractiveToggle>());
                checkBoxSet.SelectedIndices.Add(i);
                i++;
            }

            domains.Add(interactionSpecs["field"].Value, domain);

            int numRows = interactionSpecs["domain"].AsArray.Count + 1;
            collection.Rows = numRows;
            collection.UpdateCollection();

            // Add the call back function to update marks visibility when any checkbox is updated.
            checkBoxSet.OnSelectionEvents.AddListener(ToggleFilterUpdated);

            // Update the results vector
            int numMarks = targetVis.markInstances.Count;
            List<bool> results = new List<bool>(new bool[numMarks]);
            for(int j = 0; j < results.Count; j++)
            {
                results[j] = true;
            }
            filterResults.Add(interactionSpecs["field"], results);

            toggleFilterInstance.transform.Translate(0, -curYOffset, 0);
            curYOffset = curYOffset + (0.08f * numRows); 
        }

        void ToggleFilterUpdated()
        {
            GameObject selectedCheckBox = EventSystem.current.currentSelectedGameObject;
            if (selectedCheckBox != null && targetVis != null)
            {
                // Update filter results for toggled data field category.
                UpdateFilterResultsForCategory(selectedCheckBox.transform.parent.name, selectedCheckBox.transform.Find("CheckBoxOutline/Label").gameObject.GetComponent<TextMesh>().text);

                targetVis.FiltersUpdated();
                Debug.Log("Filter updated! " +
                EventSystem.current.currentSelectedGameObject.transform.parent.name);
            }
        }

        private void UpdateFilterResultsForCategory(string field, string category)
        {
            GameObject toggleFilter = gameObject.transform.Find(field).gameObject;
            if (toggleFilter == null) return;

            HoloToolkit.Examples.InteractiveElements.InteractiveSet checkBoxSet =
                toggleFilter.GetComponent<HoloToolkit.Examples.InteractiveElements.InteractiveSet>();
            if (checkBoxSet == null) return;

            List<string> visibleCategories = new List<string>();
            foreach (int checkedCategoryIndex in checkBoxSet.SelectedIndices)
            {
                visibleCategories.Add(domains[field][checkedCategoryIndex]);

                Debug.Log("showing index: " + checkedCategoryIndex.ToString() + (domains[field][checkedCategoryIndex]));
            }

            Debug.Log("Updating filter results for field, category " + field + ", " + category);
            List<bool> res = filterResults[field];
            for(int b = 0; b < res.Count; b++)
            {
                if(visibleCategories.Contains(targetVis.markInstances[b].GetComponent<Mark>().datum[field]))
                {
                    res[b] = true;
                } else
                {
                    res[b] = false;
                }
            }

            filterResults[field] = res;
        }
    }
}